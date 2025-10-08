using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class AllVideosPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private bool isLoading;

        public ObservableCollection<MediaViewModel> Videos { get; }

        private readonly ILibraryService libraryService;
        private readonly DispatcherQueue dispatcherQueue;
        private readonly DispatcherQueueTimer timer;

        public AllVideosPageViewModel(ILibraryService libraryService)
        {
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            timer = dispatcherQueue.CreateTimer();
            this.libraryService = libraryService;
            this.libraryService.VideosLibraryContentChanged += OnVideosLibraryContentChanged;
            Videos = new ObservableCollection<MediaViewModel>();
        }

        public void UpdateVideos()
        {
            IsLoading = libraryService.IsLoadingVideos;
            IReadOnlyList<MediaViewModel> videos = libraryService.GetVideosFetchResult();
            if (videos.Count < 5000)
            {
                // Only sync when the number of items is low enough
                // Sync on too many items can cause UI hang
                Videos.SyncItems(videos);
            }
            else
            {
                Videos.Clear();
                foreach (MediaViewModel video in videos)
                {
                    Videos.Add(video);
                }
            }

            // Progressively update when it's still loading
            if (IsLoading)
            {
                timer.Debounce(UpdateVideos, TimeSpan.FromSeconds(5));
            }
            else
            {
                timer.Stop();
            }
        }

        private void OnVideosLibraryContentChanged(ILibraryService sender, object args)
        {
            dispatcherQueue.TryEnqueue(UpdateVideos);
        }

        [RelayCommand]
        private void Play(MediaViewModel media)
        {
            if (Videos.Count == 0) return;
            Messenger.SendQueueAndPlay(media, Videos, true);
        }
    }
}
