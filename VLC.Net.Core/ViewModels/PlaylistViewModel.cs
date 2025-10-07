#nullable enable

using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using VLC.Net.Core.Enums;
using VLC.Net.Core.Messages;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class PlaylistViewModel : ObservableRecipient
    {
        public MediaListViewModel Playlist { get; }

        public bool HasItems
        {
            get => hasItems;
            private set
            {
                SetProperty(ref hasItems, value);
            }
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(Screenbox.Core.ViewModels.PlaylistViewModel.PlaySelectedNextCommand))]
        [NotifyCanExecuteChangedFor(nameof(Screenbox.Core.ViewModels.PlaylistViewModel.RemoveSelectedCommand))]
        [NotifyCanExecuteChangedFor(nameof(Screenbox.Core.ViewModels.PlaylistViewModel.MoveSelectedItemUpCommand))]
        [NotifyCanExecuteChangedFor(nameof(Screenbox.Core.ViewModels.PlaylistViewModel.MoveSelectedItemDownCommand))]
        private int selectionCount;

        [ObservableProperty] private bool enableMultiSelect;

        private bool hasItems;

        private readonly IFilesService filesService;
        private readonly IResourceService resourceService;
        private readonly DispatcherQueue dispatcherQueue;

        public PlaylistViewModel(MediaListViewModel playlist, IFilesService filesService, IResourceService resourceService)
        {
            Playlist = playlist;
            this.filesService = filesService;
            this.resourceService = resourceService;
            hasItems = playlist.Items.Count > 0;
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            Playlist.Items.CollectionChanged += ItemsOnCollectionChanged;
        }

        private void ItemsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            HasItems = Playlist.Items.Count > 0;
            if (!HasItems)
            {
                EnableMultiSelect = false;
            }
        }

        partial void OnEnableMultiSelectChanged(bool value)
        {
            if (!value)
                SelectionCount = 0;
        }

        private static bool HasSelection(IList<object>? selectedItems) => selectedItems?.Count > 0;

        private bool IsSelectedItemNotFirst(IList<object>? selectedItems) =>
            selectedItems?.Count == 1 &&
            Playlist.Items.Count > 0 && Playlist.Items[0] != selectedItems[0];

        private bool IsSelectedItemNotLast(IList<object>? selectedItems) =>
            selectedItems?.Count == 1 &&
            Playlist.Items.Count > 0 && Playlist.Items[Playlist.Items.Count - 1] != selectedItems[0];

        private bool IsItemNotFirst(MediaViewModel item) => Playlist.Items.Count > 0 && Playlist.Items[0] != item;

        private bool IsItemNotLast(MediaViewModel item) => Playlist.Items.Count > 0 && Playlist.Items[Playlist.Items.Count - 1] != item;

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void RemoveSelected(IList<object>? selectedItems)
        {
            if (selectedItems == null) return;
            List<object> copy = selectedItems.ToList();
            selectedItems.Clear();
            foreach (MediaViewModel item in copy)
            {
                Remove(item);
            }
        }

        [RelayCommand]
        private void Remove(MediaViewModel item)
        {
            if (Playlist.CurrentItem == item)
            {
                Playlist.CurrentItem = null;
            }

            Playlist.Items.Remove(item);
        }

        [RelayCommand]
        private void PlaySingle(MediaViewModel media)
        {
            if (Playlist.CurrentItem == media && (media.IsPlaying ?? false))
            {
                Messenger.Send(new TogglePlayPauseMessage(false));
            }
            else
            {
                Messenger.Send(new PlayMediaMessage(media, true));
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelection))]
        private void PlaySelectedNext(IList<object>? selectedItems)
        {
            if (selectedItems == null) return;
            List<object> reverse = selectedItems.Reverse().ToList();
            selectedItems.Clear();
            foreach (MediaViewModel item in reverse)
            {
                PlayNext(item);
            }
        }

        [RelayCommand]
        private void PlayNext(MediaViewModel item)
        {
            Playlist.Items.Insert(Playlist.CurrentIndex + 1, new MediaViewModel(item));
        }

        [RelayCommand(CanExecute = nameof(IsSelectedItemNotFirst))]
        private void MoveSelectedItemUp(IList<object>? selectedItems)
        {
            if (selectedItems is not { Count: 1 }) return;
            MediaViewModel item = (MediaViewModel)selectedItems[0];
            MoveItemUp(item);

            // Selected items will be empty after move
            // Delay adding the items back to selected so the items have the chance to update first
            // If this order is not followed, the whole listview will reload
            dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => selectedItems.Add(item));
        }

        [RelayCommand(CanExecute = nameof(IsItemNotFirst))]
        private void MoveItemUp(MediaViewModel item)
        {
            int index = Playlist.Items.IndexOf(item);
            if (index <= 0) return;
            Playlist.Items.RemoveAt(index);
            Playlist.Items.Insert(index - 1, item);
        }

        [RelayCommand(CanExecute = nameof(IsSelectedItemNotLast))]
        private void MoveSelectedItemDown(IList<object>? selectedItems)
        {
            if (selectedItems is not { Count: 1 }) return;
            MediaViewModel item = (MediaViewModel)selectedItems[0];
            MoveItemDown(item);

            // Selected items will be empty after move
            // Delay adding the items back to selected so the items have the chance to update first
            // If this order is not followed, the whole listview will reload
            dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => selectedItems.Add(item));
        }

        [RelayCommand(CanExecute = nameof(IsItemNotLast))]
        private void MoveItemDown(MediaViewModel item)
        {
            int index = Playlist.Items.IndexOf(item);
            if (index == -1 || index >= Playlist.Items.Count - 1) return;
            Playlist.Items.RemoveAt(index);
            Playlist.Items.Insert(index + 1, item);
        }

        [RelayCommand]
        private void ClearSelection()
        {
            EnableMultiSelect = false;
            SelectionCount = 0;
        }

        [RelayCommand]
        private async Task AddFilesAsync()
        {
            try
            {
                IReadOnlyList<StorageFile>? files = await filesService.PickMultipleFilesAsync();
                if (files == null || files.Count == 0) return;
                await Playlist.EnqueueAsync(files);
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(
                    resourceService.GetString(ResourceName.FailedToOpenFilesNotificationTitle), e.Message));
            }
        }
    }
}
