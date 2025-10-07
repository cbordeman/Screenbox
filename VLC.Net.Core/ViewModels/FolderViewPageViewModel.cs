#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using VLC.Net.Core.Common;
using VLC.Net.Core.Factories;
using VLC.Net.Core.Helpers;
using VLC.Net.Core.Messages;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    public partial class FolderViewPageViewModel : ObservableRecipient,
        IRecipient<RefreshFolderMessage>
    {
        public string TitleText => Breadcrumbs.LastOrDefault()?.Name ?? string.Empty;

        public ObservableCollection<StorageItemViewModel> Items { get; }

        public IReadOnlyList<StorageFolder> Breadcrumbs { get; private set; }

        internal NavigationMetadata? NavData { get; private set; }

        [ObservableProperty] private bool isEmpty;
        [ObservableProperty] private bool isLoading;

        private readonly IFilesService filesService;
        private readonly INavigationService navigationService;
        private readonly StorageItemViewModelFactory storageVmFactory;
        private readonly DispatcherQueue dispatcherQueue;
        private readonly DispatcherQueueTimer loadingTimer;
        private readonly List<MediaViewModel> playableItems;
        private bool isActive;
        private object? source;

        public FolderViewPageViewModel(IFilesService filesService, INavigationService navigationService,
            StorageItemViewModelFactory storageVmFactory)
        {
            this.filesService = filesService;
            this.storageVmFactory = storageVmFactory;
            this.navigationService = navigationService;
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            loadingTimer = dispatcherQueue.CreateTimer();
            playableItems = new List<MediaViewModel>();
            Breadcrumbs = Array.Empty<StorageFolder>();
            Items = new ObservableCollection<StorageItemViewModel>();

            IsActive = true;
        }

        public void Receive(RefreshFolderMessage message)
        {
            if (!isActive) return;
            dispatcherQueue.TryEnqueue(RefreshFolderContent);
        }

        public async Task OnNavigatedTo(object? parameter)
        {
            isActive = true;
            source = parameter;
            NavData = parameter as NavigationMetadata;
            await FetchContentAsync(NavData?.Parameter ?? parameter);
        }

        public void OnBreadcrumbBarItemClicked(int index)
        {
            IReadOnlyList<StorageFolder> crumbs = Breadcrumbs.Take(index + 1).ToArray();
            if (NavData != null)
            {
                if (index == 0)
                {
                    navigationService.Navigate(NavData.RootViewModelType);
                }
                else
                {
                    navigationService.Navigate(typeof(FolderViewPageViewModel),
                        new NavigationMetadata(NavData.RootViewModelType, crumbs));
                }
            }
            else
            {
                navigationService.Navigate(typeof(FolderViewPageViewModel),
                    new NavigationMetadata(typeof(FolderViewPageViewModel), crumbs));
            }
        }

        private async Task FetchContentAsync(object? parameter)
        {
            switch (parameter)
            {
                case IReadOnlyList<StorageFolder> { Count: > 0 } breadcrumbs:
                    Breadcrumbs = breadcrumbs;
                    await FetchFolderContentAsync(breadcrumbs.Last());
                    break;
                case StorageLibrary library:
                    await FetchFolderContentAsync(library);
                    break;
                case StorageFileQueryResult queryResult:
                    await FetchQueryItemAsync(queryResult);
                    break;
                case "VideosLibrary":   // Special case for VideosPage
                    // VideosPage needs to serialize navigation state so it cannot set nav data
                    try
                    {
                        Breadcrumbs = new[] { SystemInformation.IsXbox ? KnownFolders.RemovableDevices : KnownFolders.VideosLibrary };
                        NavData = new NavigationMetadata(typeof(VideosPageViewModel), Breadcrumbs);
                        await FetchFolderContentAsync(Breadcrumbs[0]);
                    }
                    catch (Exception)
                    {
                        // pass
                    }
                    break;
            }
        }

        public void OnNavigatedFrom()
        {
            isActive = false;
        }

        protected virtual void Navigate(object? parameter = null)
        {
            // _navigationService.NavigateExisting(typeof(FolderViewPageViewModel), parameter);
            navigationService.Navigate(typeof(FolderViewPageViewModel),
                new NavigationMetadata(NavData?.RootViewModelType ?? typeof(FolderViewPageViewModel), parameter));
        }

        [RelayCommand]
        private void Play(StorageItemViewModel item)
        {
            if (item.Media == null) return;
            Messenger.SendQueueAndPlay(item.Media, playableItems, true);
        }

        [RelayCommand]
        private void PlayNext(StorageItemViewModel item)
        {
            if (item.Media == null) return;
            Messenger.SendPlayNext(item.Media);
        }

        [RelayCommand]
        private void Click(StorageItemViewModel item)
        {
            if (item.Media != null)
            {
                Play(item);
            }
            else if (item.StorageItem is StorageFolder folder)
            {
                StorageFolder[] crumbs = Breadcrumbs.Append(folder).ToArray();
                Navigate(crumbs);
            }
        }

        private async Task FetchQueryItemAsync(StorageFileQueryResult query)
        {
            Items.Clear();
            playableItems.Clear();

            uint fetchIndex = 0;
            while (isActive)
            {
                loadingTimer.Debounce(() => IsLoading = true, TimeSpan.FromMilliseconds(800));
                IReadOnlyList<StorageFile> items = await query.GetFilesAsync(fetchIndex, 30);
                if (items.Count == 0) break;
                fetchIndex += (uint)items.Count;
                foreach (StorageFile storageFile in items)
                {
                    StorageItemViewModel item = storageVmFactory.GetInstance(storageFile);
                    Items.Add(item);
                    if (item.Media != null) playableItems.Add(item.Media);
                }
            }

            loadingTimer.Stop();
            IsLoading = false;
            IsEmpty = Items.Count == 0;
        }

        private async Task FetchFolderContentAsync(StorageFolder folder)
        {
            Items.Clear();
            playableItems.Clear();

            StorageItemQueryResult itemQuery = filesService.GetSupportedItems(folder);
            uint fetchIndex = 0;
            while (isActive)
            {
                loadingTimer.Debounce(() => IsLoading = true, TimeSpan.FromMilliseconds(800));
                IReadOnlyList<IStorageItem> items = await itemQuery.GetItemsAsync(fetchIndex, 30);
                if (items.Count == 0) break;
                fetchIndex += (uint)items.Count;
                foreach (IStorageItem storageItem in items)
                {
                    StorageItemViewModel item = storageVmFactory.GetInstance(storageItem);
                    Items.Add(item);
                    if (item.Media != null) playableItems.Add(item.Media);
                }
            }

            loadingTimer.Stop();
            IsLoading = false;
            IsEmpty = Items.Count == 0;
        }

        private async Task FetchFolderContentAsync(StorageLibrary library)
        {
            if (library.Folders.Count <= 0)
            {
                IsEmpty = true;
                return;
            }

            if (library.Folders.Count == 1)
            {
                // StorageLibrary is always the root
                // Fetch content of the only folder if applicable
                StorageFolder folder = library.Folders[0];
                Breadcrumbs = new[] { folder };
                await FetchFolderContentAsync(folder);
            }
            else
            {
                Items.Clear();
                foreach (StorageFolder folder in library.Folders)
                {
                    StorageItemViewModel item = storageVmFactory.GetInstance(folder);
                    Items.Add(item);
                    await item.UpdateCaptionAsync();
                }

                IsEmpty = Items.Count == 0;
            }
        }

        private async void RefreshFolderContent()
        {
            await FetchContentAsync(NavData?.Parameter ?? source);
        }
    }
}
