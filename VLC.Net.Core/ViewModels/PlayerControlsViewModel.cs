#nullable enable

using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using VLC.Net.Core.Enums;
using VLC.Net.Core.Events;
using VLC.Net.Core.Helpers;
using VLC.Net.Core.Messages;
using VLC.Net.Core.Playback;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class PlayerControlsViewModel : ObservableRecipient,
        IRecipient<MediaPlayerChangedMessage>,
        IRecipient<SettingsChangedMessage>,
        IRecipient<TogglePlayPauseMessage>,
        IRecipient<PropertyChangedMessage<PlayerVisibilityState>>
    {
        public MediaListViewModel Playlist { get; }

        public bool ShouldBeAdaptive => !IsCompact && SystemInformation.IsDesktop;

        public double SubtitleTimingOffset
        {
            // Special access. Consider promote to proper IMediaPlayer property
            get => subtitleTimingOffset = (mediaPlayer as VlcMediaPlayer)?.VlcPlayer.SpuDelay / 1000 ?? 0;
            set
            {
                if (mediaPlayer is VlcMediaPlayer player)
                {
                    // LibVLC subtitle delay is in microseconds, convert to milliseconds with multiplication by 1000
                    player.VlcPlayer.SetSpuDelay((long)(value * 1000));
                    SetProperty(ref subtitleTimingOffset, value);
                }
            }
        }

        /// <summary>
        /// This 64-bit signed integer changes the current audio delay.
        /// </summary>
        public double AudioTimingOffset
        {
            // Special access. Consider promote to proper IMediaPlayer property
            get => audioTimingOffset = (mediaPlayer as VlcMediaPlayer)?.VlcPlayer.AudioDelay / 1000 ?? 0;
            set
            {
                if (mediaPlayer is VlcMediaPlayer player)
                {
                    // LibVLC audio delay is in microseconds, convert to milliseconds with multiplication by 1000
                    player.VlcPlayer.SetAudioDelay((long)(value * 1000));
                    SetProperty(ref audioTimingOffset, value);
                }
            }
        }

        [ObservableProperty] private bool isPlaying;
        [ObservableProperty] private bool isFullscreen;
        [ObservableProperty] private string? titleName; // TODO: Handle VLC title name
        [ObservableProperty] private string? chapterName;
        [ObservableProperty] private double playbackSpeed;
        [ObservableProperty] private bool isAdvancedModeActive;
        [ObservableProperty] private bool isMinimal;
        [ObservableProperty] private bool playerShowChapters;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShouldBeAdaptive))]
        private bool isCompact;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(Screenbox.Core.ViewModels.PlayerControlsViewModel.SaveSnapshotCommand))]
        private bool hasVideo;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(Screenbox.Core.ViewModels.PlayerControlsViewModel.PlayPauseCommand))]
        private bool hasActiveItem;


        private readonly DispatcherQueue dispatcherQueue;
        private readonly IWindowService windowService;
        private readonly IResourceService resourceService;
        private readonly ISettingsService settingsService;
        private IMediaPlayer? mediaPlayer;
        private Size aspectRatio;
        private double subtitleTimingOffset;
        private double audioTimingOffset;

        public PlayerControlsViewModel(
            MediaListViewModel playlist,
            ISettingsService settingsService,
            IWindowService windowService,
            IResourceService resourceService)
        {
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            this.windowService = windowService;
            this.resourceService = resourceService;
            this.settingsService = settingsService;
            this.windowService.ViewModeChanged += WindowServiceOnViewModeChanged;
            playbackSpeed = 1.0;
            isAdvancedModeActive = settingsService.AdvancedMode;
            isMinimal = true;
            playerShowChapters = settingsService.PlayerShowChapters;
            Playlist = playlist;
            Playlist.PropertyChanged += PlaylistViewModelOnPropertyChanged;

            IsActive = true;
        }

        public void Receive(SettingsChangedMessage message)
        {
            switch (message.SettingsName)
            {
                case nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.AdvancedMode):
                    IsAdvancedModeActive = settingsService.AdvancedMode;
                    break;
                case nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.PlayerShowChapters):
                    PlayerShowChapters = settingsService.PlayerShowChapters;
                    break;
            }
        }

        public void Receive(MediaPlayerChangedMessage message)
        {
            mediaPlayer = message.Value;
            mediaPlayer.PlaybackStateChanged += OnPlaybackStateChanged;
            mediaPlayer.ChapterChanged += OnChapterChanged;
            mediaPlayer.NaturalVideoSizeChanged += OnNaturalVideoSizeChanged;
        }

        public void Receive(TogglePlayPauseMessage message)
        {
            if (!HasActiveItem || mediaPlayer == null) return;
            if (message.ShowBadge)
            {
                PlayPauseWithBadge();
            }
            else
            {
                PlayPause();
            }

        }

        public void Receive(PropertyChangedMessage<PlayerVisibilityState> message)
        {
            IsMinimal = message.NewValue != PlayerVisibilityState.Visible;
        }

        public void PlayPauseKeyboardAccelerator_OnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (args.KeyboardAccelerator.Key == VirtualKey.Space && IsMinimal) return;

            // Override default keyboard accelerator to show badge
            args.Handled = true;
            PlayPauseWithBadge();
        }

        public void ToggleSubtitle(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (mediaPlayer?.PlaybackItem == null) return;
            PlaybackSubtitleTrackList subtitleTracks = mediaPlayer.PlaybackItem.SubtitleTracks;
            if (subtitleTracks.Count == 0) return;
            args.Handled = true;
            switch (args.KeyboardAccelerator.Modifiers)
            {
                case VirtualKeyModifiers.None when subtitleTracks.Count == 1:
                    if (subtitleTracks.SelectedIndex >= 0)
                    {
                        subtitleTracks.SelectedIndex = -1;
                    }
                    else
                    {
                        subtitleTracks.SelectedIndex = 0;
                    }

                    break;

                case VirtualKeyModifiers.Control:
                    if (subtitleTracks.SelectedIndex == subtitleTracks.Count - 1)
                    {
                        subtitleTracks.SelectedIndex = -1;
                    }
                    else
                    {
                        subtitleTracks.SelectedIndex++;
                    }

                    break;

                case VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift:
                    if (subtitleTracks.SelectedIndex == -1)
                    {
                        subtitleTracks.SelectedIndex = subtitleTracks.Count - 1;
                    }
                    else
                    {
                        subtitleTracks.SelectedIndex--;
                    }

                    break;

                default:
                    args.Handled = false;
                    return;
            }

            string status = subtitleTracks.SelectedIndex == -1
                ? resourceService.GetString(ResourceName.SubtitleStatus, resourceService.GetString(ResourceName.None))
                : resourceService.GetString(ResourceName.SubtitleStatus,
                    subtitleTracks[subtitleTracks.SelectedIndex].Label);

            Messenger.Send(new UpdateStatusMessage(status));
        }

        partial void OnPlaybackSpeedChanged(double value)
        {
            if (mediaPlayer == null) return;
            mediaPlayer.PlaybackRate = value;
        }

        private void PlaylistViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Screenbox.Core.ViewModels.MediaListViewModel.CurrentItem):
                    HasActiveItem = Playlist.CurrentItem != null;
                    SubtitleTimingOffset = 0;
                    AudioTimingOffset = 0;
                    break;
            }
        }

        private void OnNaturalVideoSizeChanged(IMediaPlayer sender, object? args)
        {
            dispatcherQueue.TryEnqueue(() => HasVideo = mediaPlayer?.NaturalVideoHeight > 0);
            SaveSnapshotCommand.NotifyCanExecuteChanged();
        }

        private void OnPlaybackStateChanged(IMediaPlayer sender, object? args)
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                IsPlaying = sender.PlaybackState == MediaPlaybackState.Playing;
            });
        }

        private void OnChapterChanged(IMediaPlayer sender, object? args)
        {
            dispatcherQueue.TryEnqueue(() => ChapterName = sender.Chapter?.Title);
        }

        private void WindowServiceOnViewModeChanged(object sender, ViewModeChangedEventArgs e)
        {
            switch (e.NewValue)
            {
                case WindowViewMode.Default:
                    IsFullscreen = false;
                    IsCompact = false;
                    break;
                case WindowViewMode.Compact:
                    IsCompact = true;
                    IsFullscreen = false;
                    break;
                case WindowViewMode.FullScreen:
                    IsFullscreen = true;
                    IsCompact = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [RelayCommand]
        private void ResetMediaPlayback()
        {
            if (mediaPlayer == null) return;
            TimeSpan pos = mediaPlayer.Position;
            MediaViewModel? item = Playlist.CurrentItem;
            Playlist.CurrentItem = null;
            Playlist.CurrentItem = item;
            dispatcherQueue.TryEnqueue(() =>
            {
                mediaPlayer.Play();
                mediaPlayer.Position = pos;
            });
        }

        [RelayCommand]
        private void SetPlaybackSpeed(double speed)
        {
            PlaybackSpeed = speed;
        }

        [RelayCommand]
        private void SetAspectRatio(string aspect)
        {
            switch (aspect)
            {
                case "Fit":
                    aspectRatio = new Size(0, 0);
                    break;
                case "Fill":
                    aspectRatio = new Size(double.NaN, double.NaN);
                    break;
                default:
                    string[] values = aspect.Split(':', StringSplitOptions.RemoveEmptyEntries);
                    if (values.Length != 2) return;
                    if (!double.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double width)) return;
                    if (!double.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double height)) return;
                    aspectRatio = new Size(width, height);
                    break;
            }

            Messenger.Send(new ChangeAspectRatioMessage(aspectRatio));
        }

        [RelayCommand]
        private async Task ToggleCompactLayoutAsync()
        {
            if (IsCompact)
            {
                await windowService.TryExitCompactLayoutAsync();
            }
            else if (mediaPlayer?.NaturalVideoHeight > 0)
            {
                double aspectRatio = mediaPlayer.NaturalVideoWidth / (double)mediaPlayer.NaturalVideoHeight;
                await windowService.TryEnterCompactLayoutAsync(new Size(240 * aspectRatio, 240));
            }
            else
            {
                await windowService.TryEnterCompactLayoutAsync(new Size(240, 240));
            }
        }

        [RelayCommand]
        private void ToggleFullscreen()
        {
            if (IsCompact) return;
            if (IsFullscreen)
            {
                windowService.ExitFullScreen();
            }
            else
            {
                windowService.TryEnterFullScreen();
            }
        }

        [RelayCommand(CanExecute = nameof(Screenbox.Core.ViewModels.PlayerControlsViewModel.HasActiveItem))]
        private void PlayPause()
        {
            if (IsPlaying)
            {
                mediaPlayer?.Pause();
            }
            else
            {
                mediaPlayer?.Play();
            }
        }

        [RelayCommand(CanExecute = nameof(Screenbox.Core.ViewModels.PlayerControlsViewModel.HasVideo))]
        private async Task SaveSnapshotAsync()
        {
            if (mediaPlayer?.PlaybackState is MediaPlaybackState.Paused or MediaPlaybackState.Playing)
            {
                try
                {
                    StorageFile file = await SaveSnapshotInternalAsync(mediaPlayer);
                    Messenger.Send(new RaiseFrameSavedNotificationMessage(file));
                }
                catch (UnauthorizedAccessException)
                {
                    Messenger.Send(new RaiseLibraryAccessDeniedNotificationMessage(KnownLibraryId.Pictures));
                }
                catch (Exception e)
                {
                    Messenger.Send(new ErrorMessage(
                        resourceService.GetString(ResourceName.FailedToSaveFrameNotificationTitle), e.ToString()));
                    // TODO: track error
                }
            }
        }

        private static async Task<StorageFile> SaveSnapshotInternalAsync(IMediaPlayer mediaPlayer)
        {
            if (mediaPlayer is not VlcMediaPlayer player)
            {
                throw new NotImplementedException("Not supported on non VLC players");
            }

            StorageFolder tempFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(
                $"snapshot_{DateTimeOffset.Now.Ticks}",
                CreationCollisionOption.FailIfExists);

            try
            {
                if (!player.VlcPlayer.TakeSnapshot(0, tempFolder.Path, 0, 0))
                    throw new Exception("VLC failed to save snapshot");

                StorageFile file = (await tempFolder.GetFilesAsync())[0];
                StorageLibrary pictureLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
                StorageFolder defaultSaveFolder = pictureLibrary.SaveFolder;
                StorageFolder destFolder =
                    await defaultSaveFolder.CreateFolderAsync("Screenbox",
                        CreationCollisionOption.OpenIfExists);
                return await file.CopyAsync(destFolder, $"Screenbox_{DateTimeOffset.Now:yyyyMMdd_HHmmss}{file.FileType}",
                    NameCollisionOption.GenerateUniqueName);
            }
            finally
            {
                await tempFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
        }

        private void PlayPauseWithBadge()
        {
            if (!HasActiveItem) return;
            Messenger.Send(new ShowPlayPauseBadgeMessage(!IsPlaying));
            PlayPause();
        }
    }
}
