#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using VLC.Net.Core.Common;
using VLC.Net.Core.Enums;
using VLC.Net.Core.Events;
using VLC.Net.Core.Helpers;
using VLC.Net.Core.Messages;
using VLC.Net.Core.Models;
using VLC.Net.Core.Playback;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class SeekBarViewModel :
        ObservableRecipient,
        IRecipient<TimeChangeOverrideMessage>,
        IRecipient<ChangeTimeRequestMessage>,
        IRecipient<PlayerControlsVisibilityChangedMessage>,
        IRecipient<PlaylistCurrentItemChangedMessage>,
        IRecipient<PropertyChangedMessage<PlayerVisibilityState>>,
        IRecipient<MediaPlayerChangedMessage>
    {
        [ObservableProperty] private double length;

        [ObservableProperty] private double time;

        [ObservableProperty] private bool isSeekable;

        [ObservableProperty] private bool bufferingVisible;

        [ObservableProperty] private double previewTime;

        [ObservableProperty] private bool shouldShowPreview;

        [ObservableProperty] private bool shouldHandleKeyDown;

        public ObservableCollection<ChapterCue> Chapters { get; }

        private TimeSpan NaturalDuration => TimeSpan.FromMilliseconds((double)Length);

        private TimeSpan Position
        {
            get => TimeSpan.FromMilliseconds((double)Time);
            set => Time = value.TotalMilliseconds;
        }

        private IMediaPlayer? mediaPlayer;

        private readonly ISettingsService settingsService;
        private readonly DispatcherQueue dispatcherQueue;
        private readonly DispatcherQueueTimer bufferingTimer;
        private readonly DispatcherQueueTimer seekTimer;
        private readonly DispatcherQueueTimer originalPositionTimer;
        private readonly LastPositionTracker lastPositionTracker;
        private TimeSpan originalPosition;
        private TimeSpan lastTrackedPosition;
        private bool timeChangeOverride;
        private MediaViewModel? currentItem;

        public SeekBarViewModel(ISettingsService settingsService, LastPositionTracker lastPositionTracker)
        {
            this.settingsService = settingsService;
            this.lastPositionTracker = lastPositionTracker;
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            bufferingTimer = dispatcherQueue.CreateTimer();
            seekTimer = dispatcherQueue.CreateTimer();
            originalPositionTimer = dispatcherQueue.CreateTimer();
            originalPositionTimer.IsRepeating = false;
            shouldShowPreview = true;
            shouldHandleKeyDown = true;
            Chapters = new ObservableCollection<ChapterCue>();

            // Activate the view model's messenger
            IsActive = true;
        }

        public void Receive(PlaylistCurrentItemChangedMessage message)
        {
            lastTrackedPosition = TimeSpan.Zero;
            currentItem = message.Value;
            if (message.Value != null && lastPositionTracker.IsLoaded)
            {
                RestoreLastPosition(message.Value);
            }
        }

        public void Receive(PropertyChangedMessage<PlayerVisibilityState> message)
        {
            ShouldHandleKeyDown = message.NewValue != PlayerVisibilityState.Visible;
        }

        public void Receive(PlayerControlsVisibilityChangedMessage message)
        {
            if (!message.Value && ShouldShowPreview)
            {
                ShouldShowPreview = false;
            }
        }

        public async void Receive(MediaPlayerChangedMessage message)
        {
            if (mediaPlayer != null)
            {
                mediaPlayer.PlaybackStateChanged -= OnPlaybackStateChanged;
                mediaPlayer.NaturalDurationChanged -= OnNaturalDurationChanged;
                mediaPlayer.PositionChanged -= OnPositionChanged;
                mediaPlayer.MediaEnded -= OnEndReached;
                mediaPlayer.BufferingStarted -= OnBufferingStarted;
                mediaPlayer.BufferingEnded -= OnBufferingEnded;
                mediaPlayer.PlaybackItemChanged -= OnPlaybackItemChanged;
                mediaPlayer.CanSeekChanged -= OnCanSeekChanged;
            }

            mediaPlayer = message.Value;
            mediaPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
            mediaPlayer.NaturalDurationChanged += OnNaturalDurationChanged;
            mediaPlayer.PositionChanged += OnPositionChanged;
            mediaPlayer.MediaEnded += OnEndReached;
            mediaPlayer.BufferingStarted += OnBufferingStarted;
            mediaPlayer.BufferingEnded += OnBufferingEnded;
            mediaPlayer.PlaybackItemChanged += OnPlaybackItemChanged;
            mediaPlayer.CanSeekChanged += OnCanSeekChanged;

            if (!lastPositionTracker.IsLoaded)
            {
                await lastPositionTracker.LoadFromDiskAsync();
                if (currentItem != null)
                {
                    RestoreLastPosition(currentItem);
                }
            }
        }

        public void Receive(TimeChangeOverrideMessage message)
        {
            timeChangeOverride = message.Value;
        }

        public void Receive(ChangeTimeRequestMessage message)
        {
            var result = UpdatePosition(message.Value, message.IsOffset, message.Debounce);
            message.Reply(result);
        }

        public void OnSeekBarPointerEvent(bool pressed)
        {
            timeChangeOverride = pressed;
        }

        public void UpdatePreviewTime(double normalizedPosition)
        {
            normalizedPosition = Math.Clamp(normalizedPosition, 0, 1);
            PreviewTime = (long)(normalizedPosition * Length);
        }

        public void OnSeekBarPointerWheelChanged(double pointerWheelDelta)
        {
            if (!IsSeekable || mediaPlayer == null) return;
            var controlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control) == CoreVirtualKeyStates.Down;
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift) == CoreVirtualKeyStates.Down;
            var delta = 5000;
            if (controlPressed) delta = 10000;
            if (shiftPressed) delta = 2000;
            var result = UpdatePosition(TimeSpan.FromMilliseconds(pointerWheelDelta > 0 ? delta : -delta), true, true);
            TimeSpan offset = result.NewPosition - result.OriginalPosition;
            string extra = $"{(offset > TimeSpan.Zero ? '+' : string.Empty)}{Humanizer.ToDuration(offset)}";
            Messenger.SendPositionStatus(result.NewPosition, result.NaturalDuration, extra);
        }

        public void OnSeekBarValueChanged(object sender, RangeBaseValueChangedEventArgs args)
        {
            var newPosition = TimeSpan.FromMilliseconds(args.NewValue);
            // Only update player position when there is a user interaction.
            // SeekBar should have OneWay binding to Time, so when Time changes and invokes
            // this handler, Time = args.NewValue. The only exception is when the change is
            // coming from user.
            // We can detect user interaction by checking if Time != args.NewValue
            if (IsSeekable && mediaPlayer != null && Math.Abs((double)(Time - args.NewValue)) > 50)
            {
                Time = args.NewValue;
                double currentMs = mediaPlayer.Position.TotalMilliseconds;
                double newDiffMs = Math.Abs(args.NewValue - currentMs);
                bool shouldUpdate = newDiffMs > 400;
                bool shouldOverride = timeChangeOverride && newDiffMs > 100;
                bool paused = mediaPlayer.PlaybackState is MediaPlaybackState.Paused or MediaPlaybackState.Buffering;
                if (shouldUpdate || paused || shouldOverride)
                {
                    SetPlayerPosition(newPosition, true);
                }
            }

            UpdateLastPosition(newPosition);
        }

        private void RestoreLastPosition(MediaViewModel media)
        {
            TimeSpan lastPosition = lastPositionTracker.GetPosition(media.Location);
            if (lastPosition <= TimeSpan.Zero) return;
            if (settingsService.RestorePlaybackPosition)
            {
                if (media.IsPlaying ?? false)
                {
                    dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => SetPlayerPosition(lastPosition, false));
                    lastTrackedPosition = TimeSpan.Zero;
                }
                else
                {
                    // Media is not seekable yet, so we need to wait for the PlaybackStateChanged event
                    lastTrackedPosition = lastPosition;
                }
            }
            else
            {
                Messenger.Send(new RaiseResumePositionNotificationMessage(lastPosition));
            }
        }

        private PositionChangedResult UpdatePosition(TimeSpan position, bool isOffset, bool debounce)
        {
            TimeSpan currentPosition = Position;
            originalPositionTimer.Debounce(() => originalPosition = currentPosition, TimeSpan.FromSeconds(1), true);

            // Assume UI thread
            Position = isOffset ? (currentPosition + position) switch
            {
                var newPosition when newPosition < TimeSpan.Zero => TimeSpan.Zero,
                var newPosition when newPosition > NaturalDuration => NaturalDuration,
                var newPosition => newPosition
            } : position;
            SetPlayerPosition(Position, debounce);

            return new PositionChangedResult(currentPosition, Position, originalPosition, NaturalDuration);
        }

        private void SetPlayerPosition(TimeSpan position, bool debounce)
        {
            if (!IsSeekable || mediaPlayer == null) return;
            if (debounce)
            {
                seekTimer.Debounce(() => mediaPlayer.Position = position, TimeSpan.FromMilliseconds(50));
            }
            else
            {
                seekTimer.Stop();
                mediaPlayer.Position = position;
            }
        }

        private void OnCanSeekChanged(IMediaPlayer sender, EventArgs args)
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                IsSeekable = sender.CanSeek;
            });
        }

        private void OnPlaybackStateChanged(IMediaPlayer sender, ValueChangedEventArgs<MediaPlaybackState> args)
        {
            if (args.NewValue is not (MediaPlaybackState.None or MediaPlaybackState.Opening) &&
                lastTrackedPosition > TimeSpan.Zero)
            {
                dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    SetPlayerPosition(lastTrackedPosition, false);
                    lastTrackedPosition = TimeSpan.Zero;
                });
            }
        }

        private void OnPlaybackItemChanged(IMediaPlayer sender, object? args)
        {
            seekTimer.Stop();
            if (sender.PlaybackItem == null)
            {
                dispatcherQueue.TryEnqueue(() =>
                {
                    IsSeekable = false;
                    Time = 0;
                    Chapters.Clear();
                });
            }
            else
            {
                dispatcherQueue.TryEnqueue(() =>
                {
                    Time = 0;
                    Chapters.Clear();
                });
            }
        }

        private void OnBufferingEnded(IMediaPlayer sender, object? args)
        {
            bufferingTimer.Stop();
            dispatcherQueue.TryEnqueue(() => BufferingVisible = false);
        }

        private void OnBufferingStarted(IMediaPlayer sender, object? args)
        {
            // When the player is paused, the following still triggers a buffering
            if (sender.Position == sender.NaturalDuration)
                return;

            // Only show buffering if it takes more than 0.5s
            bufferingTimer.Debounce(() => BufferingVisible = true, TimeSpan.FromSeconds(0.5));
        }

        private void OnPositionChanged(IMediaPlayer sender, object? args)
        {
            if (seekTimer.IsRunning || timeChangeOverride) return;
            dispatcherQueue.TryEnqueue(() =>
            {
                Time = sender.Position.TotalMilliseconds;
            });
        }

        private void OnNaturalDurationChanged(IMediaPlayer sender, object? args)
        {
            // Natural duration can fluctuate during playback
            // Do not rely on this event to detect media changes
            dispatcherQueue.TryEnqueue(() =>
            {
                Length = sender.NaturalDuration.TotalMilliseconds;
                IsSeekable = sender.CanSeek;
                UpdateChapters(sender.PlaybackItem?.Chapters);
            });
        }

        private void OnEndReached(IMediaPlayer sender, object? args)
        {
            if (!timeChangeOverride)
            {
                dispatcherQueue.TryEnqueue(() =>
                {
                    // Check if Time is close enough to Length. Sometimes a new file is already loaded at this point.
                    if (Length - Time is > 0 and < 400)
                    {
                        // Round Time to Length to avoid gap at the end
                        Time = Length;
                    }
                });
            }
        }

        private void UpdateChapters(PlaybackChapterList? chapterList)
        {
            Chapters.Clear();
            if (chapterList == null) return;
            if (mediaPlayer != null)
            {
                chapterList.Load(mediaPlayer);
            }

            foreach (ChapterCue chapterCue in chapterList)
            {
                Chapters.Add(chapterCue);
            }
            // Chapters.SyncItems(chapterList);
        }

        private void UpdateLastPosition(TimeSpan position)
        {
            if (currentItem == null || NaturalDuration <= TimeSpan.FromMinutes(1) ||
                DateTimeOffset.Now - lastPositionTracker.LastUpdated <= TimeSpan.FromSeconds(3))
                return;

            if (position > TimeSpan.FromSeconds(30) && position + TimeSpan.FromSeconds(10) < NaturalDuration)
            {
                lastPositionTracker.UpdateLastPosition(currentItem.Location, position);
            }
            else if (position > TimeSpan.FromSeconds(5))
            {
                lastPositionTracker.RemovePosition(currentItem.Location);
            }
        }
    }
}
