#nullable enable

using VLC.Net.Core.Common;
using VLC.Net.Core.Factories;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    // To support navigation type matching
    public sealed class FolderListViewPageViewModel : FolderViewPageViewModel
    {
        private readonly INavigationService navigationService;

        public FolderListViewPageViewModel(IFilesService filesService,
            INavigationService navigationService,
            StorageItemViewModelFactory storageVmFactory) :
            base(filesService, navigationService, storageVmFactory)
        {
            this.navigationService = navigationService;
        }

        protected override void Navigate(object? parameter = null)
        {
            navigationService.NavigateExisting(typeof(FolderListViewPageViewModel),
                new NavigationMetadata(NavData?.RootViewModelType ?? typeof(FolderListViewPageViewModel), parameter));
        }
    }
}
