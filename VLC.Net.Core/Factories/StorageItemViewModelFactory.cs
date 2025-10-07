using VLC.Net.Core.Services;
using StorageItemViewModel = VLC.Net.Core.ViewModels.StorageItemViewModel;

namespace VLC.Net.Core.Factories
{
    public sealed class StorageItemViewModelFactory
    {
        private readonly IFilesService filesService;
        private readonly MediaViewModelFactory mediaFactory;

        public StorageItemViewModelFactory(IFilesService filesService, MediaViewModelFactory mediaFactory)
        {
            this.filesService = filesService;
            this.mediaFactory = mediaFactory;
        }

        public StorageItemViewModel GetInstance(IStorageItem storageItem)
        {
            return new StorageItemViewModel(filesService, mediaFactory, storageItem);
        }
    }
}
