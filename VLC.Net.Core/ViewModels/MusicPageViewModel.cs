#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using VLC.Net.Core.Enums;
using VLC.Net.Core.Messages;
using VLC.Net.Core.Models;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class MusicPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private bool hasContent;

        private bool LibraryLoaded => libraryService.MusicLibrary != null;

        private readonly ILibraryService libraryService;
        private readonly IResourceService resourceService;
        private readonly DispatcherQueue dispatcherQueue;

        public MusicPageViewModel(ILibraryService libraryService, IResourceService resourceService)
        {
            this.libraryService = libraryService;
            this.resourceService = resourceService;
            this.libraryService.MusicLibraryContentChanged += OnMusicLibraryContentChanged;
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            hasContent = true;
        }

        public void UpdateSongs()
        {
            MusicLibraryFetchResult musicLibrary = libraryService.GetMusicFetchResult();
            HasContent = musicLibrary.Songs.Count > 0 || libraryService.IsLoadingMusic;
            AddFolderCommand.NotifyCanExecuteChanged();
        }

        private void OnMusicLibraryContentChanged(ILibraryService sender, object args)
        {
            dispatcherQueue.TryEnqueue(UpdateSongs);
        }

        [RelayCommand(CanExecute = nameof(LibraryLoaded))]
        private async Task AddFolder()
        {
            try
            {
                await libraryService.MusicLibrary?.RequestAddFolderAsync();
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(
                    resourceService.GetString(ResourceName.FailedToAddFolderNotificationTitle), e.Message));
            }
        }
    }
}
