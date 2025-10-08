#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using VLC.Net.Core.Common;
using VLC.Net.Core.Enums;
using VLC.Net.Core.Messages;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class CommonViewModel : ObservableRecipient,
        IRecipient<SettingsChangedMessage>,
        IRecipient<PropertyChangedMessage<NavigationViewDisplayMode>>,
        IRecipient<PropertyChangedMessage<PlayerVisibilityState>>
    {
        public Dictionary<Type, string> NavigationStates { get; }

        public bool IsAdvancedModeEnabled => settingsService.AdvancedMode;

        [ObservableProperty] private NavigationViewDisplayMode navigationViewDisplayMode;
        [ObservableProperty] private Thickness scrollBarMargin;
        [ObservableProperty] private Thickness footerBottomPaddingMargin;
        [ObservableProperty] private double footerBottomPaddingHeight;

        private readonly INavigationService navigationService;
        private readonly IFilesService filesService;
        private readonly IResourceService resourceService;
        private readonly ISettingsService settingsService;
        private readonly Dictionary<string, object> pageStates;

        public CommonViewModel(INavigationService navigationService,
            IFilesService filesService,
            IResourceService resourceService,
            ISettingsService settingsService)
        {
            this.navigationService = navigationService;
            this.filesService = filesService;
            this.resourceService = resourceService;
            this.settingsService = settingsService;
            navigationViewDisplayMode = Messenger.Send<NavigationViewDisplayModeRequestMessage>();
            NavigationStates = new Dictionary<Type, string>();
            pageStates = new Dictionary<string, object>();

            // Activate the view model's messenger
            IsActive = true;
        }

        public void Receive(SettingsChangedMessage message)
        {
            if (message.SettingsName == nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.Theme) &&
                Window.Current.Content is Frame rootFrame)
            {
                rootFrame.RequestedTheme = settingsService.Theme.ToElementTheme();
            }
        }

        public void Receive(PropertyChangedMessage<NavigationViewDisplayMode> message)
        {
            this.NavigationViewDisplayMode = message.NewValue;
        }

        public void Receive(PropertyChangedMessage<PlayerVisibilityState> message)
        {
            ScrollBarMargin = message.NewValue == PlayerVisibilityState.Hidden
                ? new Thickness(0)
                : (Thickness)Application.Current.Resources["ContentPageScrollBarMargin"];

            FooterBottomPaddingMargin = message.NewValue == PlayerVisibilityState.Hidden
                ? new Thickness(0)
                : (Thickness)Application.Current.Resources["ContentPageBottomMargin"];

            FooterBottomPaddingHeight = message.NewValue == PlayerVisibilityState.Hidden
                ? 0
                : (double)Application.Current.Resources["ContentPageBottomPaddingHeight"];
        }

        public void SavePageState(object state, string pageTypeName, int backStackDepth)
        {
            pageStates[pageTypeName + backStackDepth] = state;
        }

        public bool TryGetPageState(string pageTypeName, int backStackDepth, out object state)
        {
            return pageStates.TryGetValue(pageTypeName + backStackDepth, out state);
        }

        [RelayCommand]
        private void PlayNext(MediaViewModel media)
        {
            Messenger.SendPlayNext(media);
        }

        [RelayCommand]
        private void OpenAlbum(AlbumViewModel? album)
        {
            if (album == null) return;
            navigationService.Navigate(typeof(AlbumDetailsPageViewModel),
                new NavigationMetadata(typeof(MusicPageViewModel), album));
        }

        [RelayCommand]
        private void OpenArtist(ArtistViewModel? artist)
        {
            if (artist == null) return;
            navigationService.Navigate(typeof(ArtistDetailsPageViewModel),
                new NavigationMetadata(typeof(MusicPageViewModel), artist));
        }

        [RelayCommand]
        private async Task OpenFilesAsync()
        {
            try
            {
                IReadOnlyList<StorageFile>? files = await filesService.PickMultipleFilesAsync();
                if (files == null || files.Count == 0) return;
                Messenger.Send(new PlayMediaMessage(files));
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(
                    resourceService.GetString(ResourceName.FailedToOpenFilesNotificationTitle), e.Message));
            }
        }
    }
}
