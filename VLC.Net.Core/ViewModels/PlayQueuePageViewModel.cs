#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using VLC.Net.Core.Enums;
using VLC.Net.Core.Factories;
using VLC.Net.Core.Messages;
using VLC.Net.Core.Services;
using IResourceService = VLC.Net.Core.Services.IResourceService;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class PlayQueuePageViewModel : ObservableRecipient
    {
        private readonly IFilesService filesService;
        private readonly IResourceService resourceService;
        private readonly MediaViewModelFactory mediaFactory;

        public PlayQueuePageViewModel(IFilesService filesService, IResourceService resourceService, MediaViewModelFactory mediaFactory)
        {
            this.filesService = filesService;
            this.mediaFactory = mediaFactory;
            this.resourceService = resourceService;
        }

        [RelayCommand]
        private void AddUrl(Uri? uri)
        {
            if (uri == null) return;
            MediaViewModel media = mediaFactory.GetTransient(uri);
            Messenger.Send(new QueuePlaylistMessage(new[] { media }));
        }

        [RelayCommand]
        private async Task AddFolderAsync()
        {
            try
            {
                StorageFolder? folder = await filesService.PickFolderAsync();
                if (folder == null) return;
                IReadOnlyList<IStorageItem> items = await filesService.GetSupportedItems(folder).GetItemsAsync();
                MediaViewModel[] files = items.OfType<StorageFile>().Select(f => mediaFactory.GetSingleton(f)).ToArray();
                if (files.Length == 0) return;
                Messenger.Send(new QueuePlaylistMessage(files));
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(
                    resourceService.GetString(ResourceName.FailedToOpenFilesNotificationTitle), e.Message));
            }
        }
    }
}
