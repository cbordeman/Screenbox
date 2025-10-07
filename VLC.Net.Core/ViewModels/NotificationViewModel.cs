#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using VLC.Net.Core.Common;
using VLC.Net.Core.Enums;
using VLC.Net.Core.Events;
using VLC.Net.Core.Messages;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class NotificationViewModel : ObservableRecipient,
        IRecipient<RaiseFrameSavedNotificationMessage>,
        IRecipient<RaiseResumePositionNotificationMessage>,
        IRecipient<RaiseLibraryAccessDeniedNotificationMessage>,
        IRecipient<MediaLoadFailedNotificationMessage>,
        IRecipient<CloseNotificationMessage>,
        IRecipient<SubtitleAddedNotificationMessage>,
        IRecipient<ErrorMessage>
    {
        [ObservableProperty] private NotificationLevel severity;

        [ObservableProperty] private string? title;

        [ObservableProperty] private string? message;

        [ObservableProperty] private object? content;

        [ObservableProperty] private bool isOpen;

        [ObservableProperty] private ButtonBase? actionButton;

        public string? ButtonContent { get; private set; }

        public RelayCommand? ActionCommand { get; private set; }

        private readonly INotificationService notificationService;
        private readonly IFilesService filesService;
        private readonly IResourceService resourceService;
        private readonly DispatcherQueue dispatcherQueue;
        private readonly DispatcherQueueTimer timer;

        public NotificationViewModel(INotificationService notificationService, IFilesService filesService,
            IResourceService resourceService)
        {
            this.notificationService = notificationService;
            this.filesService = filesService;
            this.resourceService = resourceService;
            this.notificationService.NotificationRaised += NotificationServiceOnNotificationRaised;
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            timer = dispatcherQueue.CreateTimer();

            // Activate the view model's messenger
            IsActive = true;
        }

        public void Receive(ErrorMessage message)
        {
            void SetNotification()
            {
                Reset();
                Title = message.Title;
                Message = message.Message;
                Severity = NotificationLevel.Error;

                IsOpen = true;
                timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(15));
            }

            dispatcherQueue.TryEnqueue(SetNotification);
        }

        public void Receive(CloseNotificationMessage message)
        {
            IsOpen = false;
        }

        public void Receive(SubtitleAddedNotificationMessage message)
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                Reset();
                Title = resourceService.GetString(ResourceName.SubtitleAddedNotificationTitle);
                Severity = NotificationLevel.Success;
                Message = message.File.Name;

                IsOpen = true;
                timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(5));
            });
        }

        public void Receive(MediaLoadFailedNotificationMessage message)
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                Reset();
                Title = resourceService.GetString(ResourceName.FailedToLoadMediaNotificationTitle);
                Severity = NotificationLevel.Error;
                Message = string.IsNullOrEmpty(message.Reason) || string.IsNullOrEmpty(message.Path)
                    ? $"{message.Path}{message.Reason}"
                    : $"{message.Path}{Environment.NewLine}{message.Reason}";

                IsOpen = true;
                timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(15));
            });
        }

        public void Receive(RaiseFrameSavedNotificationMessage message)
        {
            void SetNotification()
            {
                Reset();
                Title = resourceService.GetString(ResourceName.FrameSavedNotificationTitle);
                Severity = NotificationLevel.Success;
                ButtonContent = message.Value.Name;
                ActionCommand = new RelayCommand(() => filesService.OpenFileLocationAsync(message.Value));

                ActionButton = new HyperlinkButton
                {
                    Content = ButtonContent,
                    Command = ActionCommand
                };

                IsOpen = true;
                timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(8));
            }

            dispatcherQueue.TryEnqueue(SetNotification);
        }

        public void Receive(RaiseResumePositionNotificationMessage message)
        {
            if (Severity == NotificationLevel.Error && IsOpen) return;
            dispatcherQueue.TryEnqueue(() =>
            {
                Reset();
                if (message.Value <= TimeSpan.Zero) return;
                Title = resourceService.GetString(ResourceName.ResumePositionNotificationTitle);
                Severity = NotificationLevel.Info;
                ButtonContent = resourceService.GetString(ResourceName.GoToPosition, Humanizer.ToDuration(message.Value));
                ActionCommand = new RelayCommand(() =>
                {
                    IsOpen = false;
                    Messenger.Send(new ChangeTimeRequestMessage(message.Value, debounce: false));
                });

                ActionButton = new Button
                {
                    Content = ButtonContent,
                    Command = ActionCommand
                };

                IsOpen = true;
                timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(15));
            });
        }

        public void Receive(RaiseLibraryAccessDeniedNotificationMessage message)
        {
            string title;
            Uri link;
            switch (message.Library)
            {
                case KnownLibraryId.Music:
                    title = resourceService.GetString(ResourceName.AccessDeniedMusicLibraryTitle);
                    link = new Uri("ms-settings:privacy-musiclibrary");
                    break;
                case KnownLibraryId.Pictures:
                    title = resourceService.GetString(ResourceName.AccessDeniedPicturesLibraryTitle);
                    link = new Uri("ms-settings:privacy-pictures");
                    break;
                case KnownLibraryId.Videos:
                    title = resourceService.GetString(ResourceName.AccessDeniedVideosLibraryTitle);
                    link = new Uri("ms-settings:privacy-videos");
                    break;
                case KnownLibraryId.Documents:
                default:
                    return;
            }

            dispatcherQueue.TryEnqueue(() =>
            {
                Reset();
                Title = title;
                Severity = NotificationLevel.Error;
                ButtonContent = resourceService.GetString(ResourceName.OpenPrivacySettingsButtonText);
                Message = resourceService.GetString(ResourceName.AccessDeniedMessage);
                ActionCommand = new RelayCommand(() =>
                {
                    IsOpen = false;
                    _ = Launcher.LaunchUriAsync(link);
                });

                ActionButton = new HyperlinkButton
                {
                    Content = ButtonContent,
                    Command = ActionCommand
                };

                IsOpen = true;
                timer.Debounce(() => IsOpen = false, TimeSpan.FromSeconds(15));
            });
        }

        [RelayCommand]
        private void Close()
        {
            // IsOpen = false;
            Messenger.Send<CloseNotificationMessage>();
        }

        private void Reset()
        {
            Title = default;
            Message = default;
            Severity = default;
            ButtonContent = default;
            ActionCommand = default;
            Content = default;
            ActionButton = default;
            IsOpen = false;
        }

        private void NotificationServiceOnNotificationRaised(object sender, NotificationRaisedEventArgs e)
        {
            void SetNotification()
            {
                Reset();
                Title = e.Title;
                Message = e.Message;
                Severity = e.Level;

                TimeSpan timeout;
                switch (e.Level)
                {
                    case NotificationLevel.Warning:
                        timeout = TimeSpan.FromSeconds(10);
                        break;
                    case NotificationLevel.Error:
                        timeout = TimeSpan.FromSeconds(15);
                        break;
                    default:
                        timeout = TimeSpan.FromSeconds(6);
                        break;
                }

                IsOpen = true;
                timer.Debounce(() => IsOpen = false, timeout);
            }

            dispatcherQueue.TryEnqueue(SetNotification);
        }
    }
}
