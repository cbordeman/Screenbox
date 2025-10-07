#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LibVLCSharp.Shared;
using VLC.Net.Core.Common;
using VLC.Net.Core.Enums;
using VLC.Net.Core.Events;
using VLC.Net.Core.Helpers;
using VLC.Net.Core.Messages;
using VLC.Net.Core.Playback;
using VLC.Net.Core.Services;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class PlayerElementViewModel : ObservableRecipient,
        IRecipient<ChangeAspectRatioMessage>,
        IRecipient<SettingsChangedMessage>,
        IRecipient<MediaPlayerRequestMessage>
    {
        public event EventHandler<EventArgs>? ClearViewRequested;

        public MediaPlayer? VlcPlayer { get; private set; }

        private readonly LibVlcService libVlcService;
        private readonly ISystemMediaTransportControlsService transportControlsService;
        private readonly ISettingsService settingsService;
        private readonly IResourceService resourceService;
        private readonly DispatcherQueue dispatcherQueue;
        private readonly DispatcherQueueTimer clickTimer;
        private readonly DisplayRequestTracker requestTracker;
        private Size viewSize;
        private Size aspectRatio;
        private VlcMediaPlayer? mediaPlayer;
        private ManipulationLock manipulationLock;
        private TimeSpan timeBeforeManipulation;
        private bool playerSeekGesture;
        private bool playerVolumeGesture;

        public PlayerElementViewModel(
            LibVlcService libVlcService,
            ISettingsService settingsService,
            ISystemMediaTransportControlsService transportControlsService,
            IResourceService resourceService)
        {
            this.libVlcService = libVlcService;
            this.settingsService = settingsService;
            this.transportControlsService = transportControlsService;
            this.resourceService = resourceService;
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            clickTimer = dispatcherQueue.CreateTimer();
            requestTracker = new DisplayRequestTracker();
            LoadSettings();

            transportControlsService.TransportControls.ButtonPressed += TransportControlsOnButtonPressed;
            transportControlsService.TransportControls.PlaybackPositionChangeRequested += TransportControlsOnPlaybackPositionChangeRequested;

            // View model does not receive any message
            IsActive = true;
        }

        public void Receive(SettingsChangedMessage message)
        {
            LoadSettings();
        }

        public void Receive(ChangeAspectRatioMessage message)
        {
            aspectRatio = message.Value;
            SetCropGeometry(message.Value);
        }

        public void Receive(MediaPlayerRequestMessage message)
        {
            message.Reply(mediaPlayer);
        }

        public void Initialize(string[] swapChainOptions)
        {
            if (mediaPlayer != null)
            {
                var player = mediaPlayer;
                player.PlaybackStateChanged -= OnPlaybackStateChanged;
                player.PositionChanged -= OnPositionChanged;
                player.MediaFailed -= OnMediaFailed;
                player.PlaybackItemChanged -= OnPlaybackItemChanged;
                DisposeMediaPlayer();
            }

            Task.Run(() =>
            {
                var args = new List<string>();
                if (settingsService.GlobalArguments.Length > 0)
                {
                    args.AddRange(settingsService.GlobalArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                }

                if (settingsService.VideoUpscale != VideoUpscaleOption.Linear)
                {
                    args.Add($"--d3d11-upscale-mode={settingsService.VideoUpscale.ToString().ToLower()}");
                }

                args.AddRange(swapChainOptions);
                VlcMediaPlayer player;
                try
                {
                    player = libVlcService.Initialize(args.ToArray());
                }
                catch (VLCException e)
                {
                    player = libVlcService.Initialize(swapChainOptions);
                    Messenger.Send(new ErrorMessage(
                        resourceService.GetString(ResourceName.FailedToInitializeNotificationTitle), e.Message));
                }

                mediaPlayer = player;
                VlcPlayer = player.VlcPlayer;
                player.PlaybackStateChanged += OnPlaybackStateChanged;
                player.PositionChanged += OnPositionChanged;
                player.MediaFailed += OnMediaFailed;
                player.PlaybackItemChanged += OnPlaybackItemChanged;
                Messenger.Send(new MediaPlayerChangedMessage(player));
            });
        }

        private void OnPlaybackItemChanged(IMediaPlayer sender, ValueChangedEventArgs<PlaybackItem?> args)
        {
            if (args.NewValue == null) ClearViewRequested?.Invoke(this, EventArgs.Empty);
        }

        public void OnClick()
        {
            if (!settingsService.PlayerTapGesture || mediaPlayer?.PlaybackItem == null) return;
            if (clickTimer.IsRunning)
            {
                clickTimer.Stop();
                return;
            }

            clickTimer.Debounce(() => Messenger.Send(new TogglePlayPauseMessage(true)), TimeSpan.FromMilliseconds(200));
        }

        public void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint? pointer = e.GetCurrentPoint((UIElement)e.OriginalSource);
            int mouseWheelDelta = pointer.Properties.MouseWheelDelta;
            int volume = Messenger.Send(new ChangeVolumeRequestMessage(mouseWheelDelta > 0 ? 5 : -5, true));
            Messenger.Send(new UpdateVolumeStatusMessage(volume));
        }

        public void VideoView_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (manipulationLock == ManipulationLock.None) return;
            Messenger.Send(new OverrideControlsHideDelayMessage(100));
            Messenger.Send(new TimeChangeOverrideMessage(false));
        }

        public void VideoView_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            const double horizontalChangePerPixel = 200;
            double horizontalChange = e.Delta.Translation.X;
            double verticalChange = e.Delta.Translation.Y;
            double horizontalCumulative = e.Cumulative.Translation.X;
            double verticalCumulative = e.Cumulative.Translation.Y;

            if (mediaPlayer != null && manipulationLock == ManipulationLock.None)
                timeBeforeManipulation = mediaPlayer.Position;

            if ((manipulationLock == ManipulationLock.Vertical ||
                manipulationLock == ManipulationLock.None && Math.Abs(verticalCumulative) >= 50) &&
                playerVolumeGesture)
            {
                manipulationLock = ManipulationLock.Vertical;
                int volume = Messenger.Send(new ChangeVolumeRequestMessage((int)-verticalChange, true));
                Messenger.Send(new UpdateVolumeStatusMessage(volume));
                return;
            }

            if ((manipulationLock == ManipulationLock.Horizontal ||
                 manipulationLock == ManipulationLock.None && Math.Abs(horizontalCumulative) >= 50) &&
                (mediaPlayer?.CanSeek ?? false) &&
                playerSeekGesture)
            {
                manipulationLock = ManipulationLock.Horizontal;
                Messenger.Send(new TimeChangeOverrideMessage(true));
                TimeSpan timeChange = TimeSpan.FromMilliseconds(horizontalChange * horizontalChangePerPixel);
                TimeSpan newTime = Messenger.Send(new ChangeTimeRequestMessage(timeChange, true)).Response.NewPosition;

                string changeText = Humanizer.ToDuration(newTime - timeBeforeManipulation);
                if (changeText[0] != '-') changeText = '+' + changeText;
                string status = $"{Humanizer.ToDuration(newTime)} ({changeText})";
                Messenger.Send(new UpdateStatusMessage(status));
            }
        }

        public void VideoView_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            manipulationLock = ManipulationLock.None;
        }

        private void OnMediaFailed(IMediaPlayer sender, object? args)
        {
            transportControlsService.ClosePlayback();
        }

        private void OnPositionChanged(IMediaPlayer sender, object? args)
        {
            transportControlsService.UpdatePlaybackPosition(sender.Position, TimeSpan.Zero, sender.NaturalDuration);
        }

        public void OnSizeChanged(object sender, SizeChangedEventArgs args)
        {
            viewSize = args.NewSize;
            SetCropGeometry(aspectRatio);
        }

        private void TransportControlsOnPlaybackPositionChangeRequested(SystemMediaTransportControls sender, PlaybackPositionChangeRequestedEventArgs args)
        {
            if (mediaPlayer == null) return;
            mediaPlayer.Position = args.RequestedPlaybackPosition;
        }

        private void TransportControlsOnButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            if (mediaPlayer == null) return;
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Pause:
                    mediaPlayer.Pause();
                    break;
                case SystemMediaTransportControlsButton.Play:
                    mediaPlayer.Play();
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    mediaPlayer.PlaybackItem = null;
                    break;
                case SystemMediaTransportControlsButton.FastForward:
                    mediaPlayer.Position += TimeSpan.FromSeconds(10);
                    break;
                case SystemMediaTransportControlsButton.Rewind:
                    mediaPlayer.Position -= TimeSpan.FromSeconds(10);
                    break;
            }
        }

        private void OnPlaybackStateChanged(IMediaPlayer sender, object? args)
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                UpdateDisplayRequest(sender.PlaybackState, requestTracker);
            });

            transportControlsService.UpdatePlaybackStatus(sender.PlaybackState);
        }

        private void SetCropGeometry(Size size)
        {
            if (mediaPlayer == null || size.Width < 0 || size.Height < 0) return;
            Rect defaultSize = new(0, 0, 1, 1);
            if (size is { Width: 0, Height: 0 })
            {
                if (mediaPlayer.NormalizedSourceRect == defaultSize) return;
                mediaPlayer.NormalizedSourceRect = defaultSize;
            }
            else
            {
                if (double.IsNaN(size.Width) || double.IsNaN(size.Height))
                {
                    size = viewSize;
                }

                double leftOffset = 0.5, topOffset = 0.5;
                double widthRatio = size.Width / mediaPlayer.NaturalVideoWidth;
                double heightRatio = size.Height / mediaPlayer.NaturalVideoHeight;
                double ratio = Math.Max(widthRatio, heightRatio);
                double width = size.Width / ratio / mediaPlayer.NaturalVideoWidth;
                double height = size.Height / ratio / mediaPlayer.NaturalVideoHeight;
                leftOffset -= width / 2;
                topOffset -= height / 2;

                mediaPlayer.NormalizedSourceRect = new Rect(leftOffset, topOffset, width, height);
            }
        }

        private void LoadSettings()
        {
            playerSeekGesture = settingsService.PlayerSeekGesture;
            playerVolumeGesture = settingsService.PlayerVolumeGesture;
        }

        private void DisposeMediaPlayer()
        {
            mediaPlayer?.Close();
            mediaPlayer?.LibVlc.Dispose();
            mediaPlayer = null;
            VlcPlayer = null;
        }

        private static void UpdateDisplayRequest(MediaPlaybackState state, DisplayRequestTracker tracker)
        {
            bool shouldActive = state
                is MediaPlaybackState.Playing
                or MediaPlaybackState.Buffering
                or MediaPlaybackState.Opening;
            if (shouldActive && !tracker.IsActive)
            {
                tracker.RequestActive();
            }
            else if (!shouldActive && tracker.IsActive)
            {
                tracker.RequestRelease();
            }
        }
    }
}
