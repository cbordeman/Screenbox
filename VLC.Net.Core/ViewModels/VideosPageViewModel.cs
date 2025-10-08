#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using VLC.Net.Core.Enums;
using VLC.Net.Core.Messages;
using VLC.Net.Core.Services;
using IResourceService = VLC.Net.Core.Services.IResourceService;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class VideosPageViewModel : ObservableRecipient
    {
        public ObservableCollection<StorageFolder> Breadcrumbs { get; }

        [ObservableProperty] private bool hasVideos;

        private bool HasLibrary => libraryService.VideosLibrary != null;

        private readonly ILibraryService libraryService;
        private readonly IResourceService resourceService;
        private readonly DispatcherQueue dispatcherQueue;

        public VideosPageViewModel(ILibraryService libraryService, IResourceService resourceService)
        {
            this.libraryService = libraryService;
            this.resourceService = resourceService;
            this.libraryService.VideosLibraryContentChanged += OnVideosLibraryContentChanged;
            hasVideos = true;
            Breadcrumbs = new ObservableCollection<StorageFolder>();
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        public void UpdateVideos()
        {
            if (Breadcrumbs.Count == 0 && TryGetFirstFolder(out StorageFolder firstFolder))
                Breadcrumbs.Add(firstFolder);
            HasVideos = libraryService.GetVideosFetchResult().Count > 0;
            AddFolderCommand.NotifyCanExecuteChanged();
        }

        public void OnContentFrameNavigated(object sender, NavigationEventArgs e)
        {
            IReadOnlyList<StorageFolder>? crumbs = e.Parameter as IReadOnlyList<StorageFolder>;
            UpdateBreadcrumbs(crumbs);
        }

        private bool TryGetFirstFolder(out StorageFolder folder)
        {
            try
            {
                folder = SystemInformation.IsXbox ? KnownFolders.RemovableDevices : KnownFolders.VideosLibrary;
                return true;
            }
            catch (Exception e)
            {
                folder = ApplicationData.Current.TemporaryFolder;
                Messenger.Send(new ErrorMessage(null, e.Message));
                LogService.Log(e);
                return false;
            }
        }

        private void UpdateBreadcrumbs(IReadOnlyList<StorageFolder>? crumbs)
        {
            Breadcrumbs.Clear();
            if (crumbs == null)
            {
                if (TryGetFirstFolder(out StorageFolder firstFolder))
                    Breadcrumbs.Add(firstFolder);
            }
            else
            {
                foreach (StorageFolder storageFolder in crumbs)
                {
                    Breadcrumbs.Add(storageFolder);
                }
            }
        }

        private void OnVideosLibraryContentChanged(ILibraryService sender, object args)
        {
            dispatcherQueue.TryEnqueue(UpdateVideos);
        }

        [RelayCommand(CanExecute = nameof(HasLibrary))]
        private async Task AddFolder()
        {
            try
            {
                await libraryService.VideosLibrary?.RequestAddFolderAsync();
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(
                    resourceService.GetString(ResourceName.FailedToAddFolderNotificationTitle), e.Message));
            }
        }
    }
}