#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using VLC.Net.Core.Enums;
using VLC.Net.Core.Events;
using VLC.Net.Core.Helpers;
using VLC.Net.Core.Messages;
using VLC.Net.Core.Models;
using VLC.Net.Core.Playback;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class PlayerPageViewModel : ObservableRecipient,
        IRecipient<UpdateStatusMessage>,
        IRecipient<UpdateVolumeStatusMessage>,
        IRecipient<TogglePlayerVisibilityMessage>,
        IRecipient<MediaPlayerChangedMessage>,
        IRecipient<PlaylistCurrentItemChangedMessage>,
        IRecipient<ShowPlayPauseBadgeMessage>,
        IRecipient<OverrideControlsHideDelayMessage>,
        IRecipient<DragDropMessage>,
        IRecipient<PropertyChangedMessage<LivelyWallpaperModel?>>,
        IRecipient<PropertyChangedMessage<NavigationViewDisplayMode>>
    {
        [ObservableProperty] private bool controlsHidden;
        [ObservableProperty] private string? statusMessage;
        [ObservableProperty] private bool isPlaying;
        [ObservableProperty] private bool isPlayingBadge;
        [ObservableProperty] private bool isOpening;
        [ObservableProperty] private bool audioOnly;
        [ObservableProperty] private bool showPlayPauseBadge;
        [ObservableProperty] private WindowViewMode viewMode;
        [ObservableProperty] private NavigationViewDisplayMode navigationViewDisplayMode;
        [ObservableProperty] private MediaViewModel? media;
        [ObservableProperty] private bool showVisualizer;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        private PlayerVisibilityState playerVisibility;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        private MediaPlaybackState playbackState;

        public bool SeekBarPointerInteracting { get; set; }

        private readonly DispatcherQueue dispatcherQueue;
        private readonly DispatcherQueueTimer openingTimer;
        private readonly DispatcherQueueTimer controlsVisibilityTimer;
        private readonly DispatcherQueueTimer statusMessageTimer;
        private readonly DispatcherQueueTimer playPauseBadgeTimer;
        private readonly IWindowService windowService;
        private readonly ISettingsService settingsService;
        private readonly IResourceService resourceService;
        private readonly IFilesService filesService;
        private IMediaPlayer? mediaPlayer;
        private bool visibilityOverride;
        private bool resizeNext;
        private DateTimeOffset lastUpdated;

        public PlayerPageViewModel(IWindowService windowService, IResourceService resourceService, ISettingsService settingsService, IFilesService filesService)
        {
            this.windowService = windowService;
            this.resourceService = resourceService;
            this.settingsService = settingsService;
            this.filesService = filesService;
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            openingTimer = dispatcherQueue.CreateTimer();
            controlsVisibilityTimer = dispatcherQueue.CreateTimer();
            statusMessageTimer = dispatcherQueue.CreateTimer();
            playPauseBadgeTimer = dispatcherQueue.CreateTimer();
            navigationViewDisplayMode = Messenger.Send<NavigationViewDisplayModeRequestMessage>();
            playerVisibility = PlayerVisibilityState.Hidden;
            lastUpdated = DateTimeOffset.MinValue;

            FocusManager.GotFocus += FocusManagerOnFocusChanged;
            this.windowService.ViewModeChanged += WindowServiceOnViewModeChanged;

            // Activate the view model's messenger
            IsActive = true;
        }

        public async void Receive(DragDropMessage message)
        {
            await OnDropAsync(message.Data);
        }

        public void Receive(PropertyChangedMessage<LivelyWallpaperModel?> message)
        {
            if (message.NewValue == null) return;
            ShowVisualizer = AudioOnly && !string.IsNullOrEmpty(message.NewValue.Path);
        }

        public void Receive(TogglePlayerVisibilityMessage message)
        {
            switch (PlayerVisibility)
            {
                case PlayerVisibilityState.Visible:
                    GoBack();
                    break;
                case PlayerVisibilityState.Minimal:
                    RestorePlayer();
                    break;
            }
        }

        public void Receive(PropertyChangedMessage<NavigationViewDisplayMode> message)
        {
            NavigationViewDisplayMode = message.NewValue;
        }

        private void WindowServiceOnViewModeChanged(object sender, ViewModeChangedEventArgs e)
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                ViewMode = e.NewValue;
            });
        }

        public void Receive(MediaPlayerChangedMessage message)
        {
            mediaPlayer = message.Value;
            mediaPlayer.PlaybackStateChanged += OnStateChanged;
            mediaPlayer.NaturalVideoSizeChanged += OnNaturalVideoSizeChanged;
        }

        public void Receive(UpdateVolumeStatusMessage message)
        {
            Receive(new UpdateStatusMessage(
                resourceService.GetString(ResourceName.VolumeChangeStatusMessage, message.Value)));
        }

        public void Receive(UpdateStatusMessage message)
        {
            // Don't show status message when player is not visible
            if (PlayerVisibility != PlayerVisibilityState.Visible && !string.IsNullOrEmpty(message.Value)) return;

            dispatcherQueue.TryEnqueue(() =>
            {
                StatusMessage = message.Value;
                if (message.Value == null)
                {
                    statusMessageTimer.Stop();
                    return;
                }

                statusMessageTimer.Debounce(() => StatusMessage = null, TimeSpan.FromSeconds(1));
            });
        }

        public void Receive(PlaylistCurrentItemChangedMessage message)
        {
            dispatcherQueue.TryEnqueue(() => ProcessOpeningMedia(message.Value));
        }

        public void Receive(ShowPlayPauseBadgeMessage message)
        {
            IsPlayingBadge = message.IsPlaying;
            BlinkPlayPauseBadge();
        }

        public void Receive(OverrideControlsHideDelayMessage message)
        {
            OverrideControlsDelayHide(message.Delay);
        }

        public async Task OnDropAsync(DataPackageView data)
        {
            try
            {
                if (data.Contains(StandardDataFormats.StorageItems))
                {
                    IReadOnlyList<IStorageItem>? items = await data.GetStorageItemsAsync();
                    if (items.Count > 0)
                    {
                        if (items.Count == 1 && items[0] is StorageFile file && file.IsSupportedSubtitle() &&
                            mediaPlayer is VlcMediaPlayer player && Media?.Item.Value != null)
                        {
                            Media.Item.Value.SubtitleTracks.AddExternalSubtitle(player, file, true);
                            Messenger.Send(new SubtitleAddedNotificationMessage(file));
                        }
                        else
                        {
                            Messenger.Send(new PlayFilesMessage(items));
                        }

                        return;
                    }
                }

                if (data.Contains(StandardDataFormats.WebLink))
                {
                    Uri? uri = await data.GetWebLinkAsync();
                    if (uri.IsFile)
                    {
                        Messenger.Send(new PlayMediaMessage(uri));
                    }
                }
            }
            catch (Exception exception)
            {
                Messenger.Send(new MediaLoadFailedNotificationMessage(exception.Message, string.Empty));
            }
        }

        public bool OnPlayerClick()
        {
            if (!ControlsHidden) return !settingsService.PlayerTapGesture && TryHideControls(true);
            ControlsHidden = false;
            DelayHideControls();
            return true;
        }

        public void OnPointerMoved()
        {
            if (visibilityOverride) return;
            ControlsHidden = false;

            if (SeekBarPointerInteracting) return;
            DelayHideControls();
        }

        public void OnPreviewSpaceKeyDown(object sender, KeyRoutedEventArgs e)
        {
            bool ctrlDown = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            bool shiftDown = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            // Only trigger with keyboard Space key and with no modifier
            if (e.OriginalKey != VirtualKey.Space || ctrlDown || shiftDown || e.KeyStatus.IsMenuKeyDown) return;
            e.Handled = true;
            // Only trigger once when Space is held down
            if (e.KeyStatus.WasKeyDown) return;
            Messenger.Send(new TogglePlayPauseMessage(true));
        }

        public void OnVolumeKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (mediaPlayer == null || sender.Modifiers != VirtualKeyModifiers.None) return;
            bool playerVisible = PlayerVisibility == PlayerVisibilityState.Visible;
            int volumeChange;
            VirtualKey key = sender.Key;

            switch (key)
            {
                case (VirtualKey)0xBB:  // Plus ("+")
                case (VirtualKey)0x6B:  // Add ("+")(Numpad plus)
                case VirtualKey.Up when playerVisible:
                    volumeChange = 5;
                    break;
                case (VirtualKey)0xBD:  // Minus ("-")
                case (VirtualKey)0x6D:  // Subtract ("-")(Numpad minus)
                case VirtualKey.Down when playerVisible:
                    volumeChange = -5;
                    break;
                default:
                    return;
            }

            int volume = Messenger.Send(new ChangeVolumeRequestMessage(volumeChange, true));
            Messenger.Send(new UpdateVolumeStatusMessage(volume));
            args.Handled = true;
        }

        public void OnSeekKeyboardAcceleratorInvoked(KeyboardAccelerator sender,
            KeyboardAcceleratorInvokedEventArgs args)
        {
            if (mediaPlayer == null) return;
            bool playerVisible = PlayerVisibility == PlayerVisibilityState.Visible;
            long seekAmount = 0;
            int direction;
            switch (sender.Key)
            {
                case VirtualKey.Left when playerVisible:
                case VirtualKey.J:
                    direction = -1;
                    break;
                case VirtualKey.Right when playerVisible:
                case VirtualKey.L:
                    direction = 1;
                    break;
                default:
                    return;
            }

            switch (sender.Modifiers)
            {
                case VirtualKeyModifiers.Control:
                    seekAmount = 10000;
                    break;
                case VirtualKeyModifiers.Shift:
                    seekAmount = 1000;
                    break;
                case VirtualKeyModifiers.None:
                    seekAmount = 5000;
                    break;
            }

            seekAmount *= direction;
            if (seekAmount != 0)
            {
                Messenger.SendSeekWithStatus(TimeSpan.FromMilliseconds(seekAmount));
            }

            args.Handled = true;
        }

        public void OnPercentJumpKeyboardAcceleratorInvoked(KeyboardAccelerator sender,
            KeyboardAcceleratorInvokedEventArgs args)
        {
            if (mediaPlayer == null || PlayerVisibility != PlayerVisibilityState.Visible) return;
            VirtualKey key = sender.Key;
            PositionChangedResult result;
            string extra = string.Empty;
            switch (key)
            {
                case VirtualKey.Home:
                    result = Messenger.Send(new ChangeTimeRequestMessage(TimeSpan.Zero));
                    break;

                case VirtualKey.End:
                    result = Messenger.Send(new ChangeTimeRequestMessage(mediaPlayer.NaturalDuration));
                    break;

                case VirtualKey.NumberPad0:
                case VirtualKey.NumberPad1:
                case VirtualKey.NumberPad2:
                case VirtualKey.NumberPad3:
                case VirtualKey.NumberPad4:
                case VirtualKey.NumberPad5:
                case VirtualKey.NumberPad6:
                case VirtualKey.NumberPad7:
                case VirtualKey.NumberPad8:
                case VirtualKey.NumberPad9:
                    int percent = (key - VirtualKey.NumberPad0) * 10;
                    TimeSpan newPosition = mediaPlayer.NaturalDuration * (0.01 * percent);
                    result = Messenger.Send(new ChangeTimeRequestMessage(newPosition));
                    extra = $"{percent}%";
                    break;

                default:
                    return;
            }

            Messenger.SendPositionStatus(result.NewPosition, result.NaturalDuration, extra);
            args.Handled = true;
        }

        public void OnPlaybackRateKeyboardAcceleratorInvoked(KeyboardAccelerator sender,
            KeyboardAcceleratorInvokedEventArgs args)
        {
            if (mediaPlayer == null || sender.Modifiers != VirtualKeyModifiers.Shift ||
                PlayerVisibility != PlayerVisibilityState.Visible) return;
            args.Handled = true;
            switch (sender.Key)
            {
                case (VirtualKey)190:   // Shift + . (">")
                    TogglePlaybackRate(true);
                    return;
                case (VirtualKey)188:   // Shift + , ("<")
                    TogglePlaybackRate(false);
                    return;
            }
        }

        public void OnFrameJumpKeyboardAcceleratorInvoked(KeyboardAccelerator sender,
            KeyboardAcceleratorInvokedEventArgs args)
        {
            if (PlayerVisibility != PlayerVisibilityState.Visible || (!(mediaPlayer?.CanSeek ?? false)) ||
                mediaPlayer.PlaybackState != MediaPlaybackState.Paused) return;
            args.Handled = true;
            switch (sender.Key)
            {
                case (VirtualKey)190:   // Period (".")
                    mediaPlayer.StepForwardOneFrame();
                    return;
                case (VirtualKey)188:   // Comma (",")
                    mediaPlayer.StepBackwardOneFrame();
                    return;
            }
        }

        public void OnManualHideControlsKeyboardAcceleratorInvoked(KeyboardAccelerator sender,
            KeyboardAcceleratorInvokedEventArgs args)
        {
            if (windowService.ViewMode != WindowViewMode.Default) return;
            if (TryHideControls())
            {
                args.Handled = true;
            }
        }

        public void OnResizeKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (mediaPlayer == null) return;
            args.Handled = true;
            Size videoSize = new(mediaPlayer.NaturalVideoWidth, mediaPlayer.NaturalVideoHeight);
            var view = ApplicationView.GetForCurrentView();
            // Visible bounds always have 1 pixel less than actual window height?
            var currentSize = new Size(view.VisibleBounds.Width, view.VisibleBounds.Height + 1);
            // Desired step is 10% of the current window size
            // However, 10% step doesn't always give a round number for resizing and rounding error will accumulate
            // We want to maintain the original aspect ratio as long as possible
            var stepHeight = Math.Round(currentSize.Height * 0.1);
            var stepWidth = Math.Round(currentSize.Width * 0.1);
            var desiredStepSize = Math.Min(stepWidth / currentSize.Width, stepHeight / currentSize.Height);
            switch (sender.Key)
            {
                case VirtualKey.Number1 when sender.Modifiers == VirtualKeyModifiers.None:
                    ResizeWindow(videoSize, 0.5);
                    break;
                case VirtualKey.Number2 when sender.Modifiers == VirtualKeyModifiers.None:
                    ResizeWindow(videoSize, 1);
                    break;
                case VirtualKey.Number3 when sender.Modifiers == VirtualKeyModifiers.None:
                    ResizeWindow(videoSize, 1.5);
                    break;
                case VirtualKey.Number4 when sender.Modifiers == VirtualKeyModifiers.None:
                    ResizeWindow(videoSize, 0);
                    break;
                case (VirtualKey)0xBB when sender.Modifiers == VirtualKeyModifiers.Control:  // Plus ("+")
                    ResizeWindow(currentSize, 1 + desiredStepSize);
                    break;
                case (VirtualKey)0xBD when sender.Modifiers == VirtualKeyModifiers.Control:  // Minus ("-")
                    ResizeWindow(currentSize, 1 - desiredStepSize);
                    break;
                default:
                    args.Handled = false;
                    break;
            }
        }

        public void OnFileLaunched()
        {
            if (settingsService.PlayerAutoResize == PlayerAutoResizeOption.OnLaunch)
                resizeNext = true;
        }

        // Hidden button acts as a focus sink when controls are hidden
        public void HiddenButtonOnClick()
        {
            ControlsHidden = false;
            if (SystemInformation.IsDesktop)
            {
                // On Desktop, user expect Space to pause without needing to see the controls
                Messenger.Send(new TogglePlayPauseMessage(true));
            }
        }

        private void TogglePlaybackRate(bool speedUp)
        {
            if (mediaPlayer == null) return;
            Span<double> steps = stackalloc[] { 0.25, 0.5, 0.75, 1, 1.25, 1.5, 1.75, 2 };
            double lastPositiveStep = steps[0];
            foreach (double step in steps)
            {
                double diff = step - mediaPlayer.PlaybackRate;
                if (speedUp && diff > 0)
                {
                    mediaPlayer.PlaybackRate = step;
                    Messenger.Send(new UpdateStatusMessage($"{step}×"));
                    return;
                }

                if (!speedUp)
                {
                    if (-diff > 0)
                    {
                        lastPositiveStep = step;
                    }
                    else
                    {
                        mediaPlayer.PlaybackRate = lastPositiveStep;
                        Messenger.Send(new UpdateStatusMessage($"{lastPositiveStep}×"));
                        return;
                    }
                }
            }
        }

        partial void OnControlsHiddenChanged(bool value)
        {
            if (value)
            {
                windowService.HideCursor();
            }
            else
            {
                windowService.ShowCursor();
            }

            Messenger.Send(new PlayerControlsVisibilityChangedMessage(!value));
        }

        partial void OnPlayerVisibilityChanged(PlayerVisibilityState value)
        {
            if (value != PlayerVisibilityState.Visible) ControlsHidden = false;
        }

        [RelayCommand]
        public void GoBack()
        {
            // Only allow back when not in fullscreen or compact overlay
            // Doing so would break layout logic
            switch (windowService.ViewMode)
            {
                case WindowViewMode.FullScreen:
                    windowService.ExitFullScreen();
                    break;
                case WindowViewMode.Compact:
                    windowService.TryExitCompactLayoutAsync();
                    break;
                case WindowViewMode.Default:
                    PlaylistInfo playlist = Messenger.Send(new PlaylistRequestMessage());
                    bool hasItemsInQueue = playlist.Playlist.Count > 0;
                    PlayerVisibility = hasItemsInQueue ? PlayerVisibilityState.Minimal : PlayerVisibilityState.Hidden;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [RelayCommand]
        private void RestorePlayer()
        {
            PlayerVisibility = PlayerVisibilityState.Visible;
        }

        private void BlinkPlayPauseBadge()
        {
            ShowPlayPauseBadge = true;
            playPauseBadgeTimer.Debounce(() => ShowPlayPauseBadge = false, TimeSpan.FromMilliseconds(100));
        }

        public bool TryHideControls(bool skipFocusCheck = false)
        {
            bool shouldCheckPlaying = settingsService.PlayerShowControls && !IsPlaying;
            if (PlayerVisibility != PlayerVisibilityState.Visible || shouldCheckPlaying ||
                SeekBarPointerInteracting || AudioOnly || ControlsHidden) return false;

            if (!skipFocusCheck)
            {
                Control? focused = FocusManager.GetFocusedElement() as Control;
                // Don't hide controls when a Slider is in focus since user can interact with Slider
                // using arrow keys without affecting focus.
                if (focused is Slider { IsFocusEngaged: true }) return false;

                // Don't hide controls when a flyout is in focus
                // Flyout is not in the same XAML tree of the Window content, use this fact to detect flyout opened
                Control? root = focused?.FindAscendant<Frame>(frame => frame == Window.Current.Content) ??
                                focused?.FindChild<Frame>(frame => frame == Window.Current.Content);
                if (root == null) return false;
            }

            ControlsHidden = true;

            // Workaround for PointerMoved is raised when show/hide cursor
            OverrideControlsDelayHide();

            return true;
        }

        private void DelayHideControls(int delayInSeconds = 3)
        {
            if (PlayerVisibility != PlayerVisibilityState.Visible || AudioOnly) return;
            controlsVisibilityTimer.Debounce(() => TryHideControls(), TimeSpan.FromSeconds(delayInSeconds));
        }

        private void OverrideControlsDelayHide(int delay = 400)
        {
            visibilityOverride = true;
            Task.Delay(delay).ContinueWith(_ => visibilityOverride = false);
        }

        private void FocusManagerOnFocusChanged(object sender, FocusManagerGotFocusEventArgs e)
        {
            if (visibilityOverride) return;
            ControlsHidden = false;
            DelayHideControls(4);
        }

        private async void ProcessOpeningMedia(MediaViewModel? current)
        {
            Media = current;
            if (current != null)
            {
                await current.LoadDetailsAsync(filesService);
                await current.LoadThumbnailAsync();
                AudioOnly = current.MediaType == MediaPlaybackType.Music;
                ShowVisualizer = AudioOnly && !string.IsNullOrEmpty(settingsService.LivelyActivePath);
                bool shouldBeVisible = settingsService.PlayerAutoResize == PlayerAutoResizeOption.Always && !AudioOnly;
                if (PlayerVisibility != PlayerVisibilityState.Visible)
                {
                    PlayerVisibility = shouldBeVisible ? PlayerVisibilityState.Visible : PlayerVisibilityState.Minimal;
                }

                if (AudioOnly)
                {
                    // If it's audio only, don't resize on next video playback
                    resizeNext = false;
                }
            }
            else if (PlayerVisibility == PlayerVisibilityState.Minimal)
            {
                PlayerVisibility = PlayerVisibilityState.Hidden;
            }
        }

        private void OnStateChanged(IMediaPlayer sender, object? args)
        {
            openingTimer.Stop();
            MediaPlaybackState state = sender.PlaybackState;
            if (state == MediaPlaybackState.Opening)
            {
                openingTimer.Debounce(() => IsOpening = state == MediaPlaybackState.Opening, TimeSpan.FromSeconds(0.5));
            }

            dispatcherQueue.TryEnqueue(() =>
            {
                PlaybackState = state;
                IsPlaying = state == MediaPlaybackState.Playing;
                IsOpening = false;

                if (!IsPlaying && settingsService.PlayerShowControls)
                {
                    ControlsHidden = false;
                }

                if (!IsPlaying && !settingsService.PlayerShowControls)
                {
                    DelayHideControls();
                }

                if (!ControlsHidden && IsPlaying)
                {
                    DelayHideControls();
                }
            });
        }

        private void OnNaturalVideoSizeChanged(IMediaPlayer sender, EventArgs args)
        {
            if (!resizeNext && settingsService.PlayerAutoResize != PlayerAutoResizeOption.Always) return;
            resizeNext = false;

            dispatcherQueue.TryEnqueue(() =>
            {
                Size desiredSize = new(sender.NaturalVideoWidth, sender.NaturalVideoHeight);
                if (ResizeWindow(desiredSize, 1)) return;

                // Resize to fill the screen only when video size is bigger than max window size
                Size maxWindowSize = windowService.GetMaxWindowSize();
                if (sender.NaturalVideoWidth >= maxWindowSize.Width ||
                    sender.NaturalVideoHeight >= maxWindowSize.Height)
                    ResizeWindow(desiredSize, 0);
            });
        }

        private bool ResizeWindow(Size desiredSize, double scalar = 1)
        {
            if (scalar < 0 || windowService.ViewMode != WindowViewMode.Default) return false;
            double actualScalar = windowService.ResizeWindow(desiredSize, scalar);
            if (actualScalar > 0)
            {
                string status = resourceService.GetString(ResourceName.ScaleStatus, $"{actualScalar * 100:0.##}%");
                Messenger.Send(new UpdateStatusMessage(status));
                return true;
            }

            return false;
        }
    }
}
