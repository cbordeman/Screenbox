#nullable enable

using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VLC.Net.Core.Helpers;
using VLC.Net.Core.Models;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class SongsPageViewModel : BaseMusicContentViewModel
    {
        public ObservableGroupedCollection<string, MediaViewModel> GroupedSongs { get; }

        [ObservableProperty]
        private string sortBy = string.Empty;

        private readonly ILibraryService libraryService;
        private readonly DispatcherQueue dispatcherQueue;
        private readonly DispatcherQueueTimer refreshTimer;

        public SongsPageViewModel(ILibraryService libraryService)
        {
            this.libraryService = libraryService;
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            refreshTimer = dispatcherQueue.CreateTimer();
            GroupedSongs = new ObservableGroupedCollection<string, MediaViewModel>();

            libraryService.MusicLibraryContentChanged += OnMusicLibraryContentChanged;
            PropertyChanged += OnPropertyChanged;
        }

        public void OnNavigatedFrom()
        {
            libraryService.MusicLibraryContentChanged -= OnMusicLibraryContentChanged;
            refreshTimer.Stop();
        }

        public void FetchSongs()
        {
            // No need to run fetch async. HomePageViewModel should already called the method.
            MusicLibraryFetchResult musicLibrary = libraryService.GetMusicFetchResult();
            IsLoading = libraryService.IsLoadingMusic;
            Songs = musicLibrary.Songs;

            // Populate song groups with fetched result
            var groups = GetCurrentGrouping(musicLibrary);
            if (Songs.Count < 5000)
            {
                // Only sync when the number of items is low enough
                // Sync on too many items can cause UI hang
                GroupedSongs.SyncObservableGroups(groups);
            }
            else
            {
                GroupedSongs.Clear();
                foreach (IGrouping<string, MediaViewModel> group in groups)
                {
                    GroupedSongs.AddGroup(group);
                }
            }

            // Progressively update when it's still loading
            if (libraryService.IsLoadingMusic)
            {
                refreshTimer.Debounce(FetchSongs, TimeSpan.FromSeconds(5));
            }
            else
            {
                refreshTimer.Stop();
            }
        }

        private List<IGrouping<string, MediaViewModel>> GetAlbumGrouping(MusicLibraryFetchResult fetchResult)
        {
            var groups = Enumerable.GroupBy<MediaViewModel, string>(Songs, m => m.Album?.Name ?? fetchResult.UnknownAlbum.Name)
                .OrderBy(g => g.Key)
                .ToList();

            var index = groups.FindIndex(g => g.Key == fetchResult.UnknownAlbum.Name);
            if (index >= 0)
            {
                var firstGroup = groups[index];
                groups.RemoveAt(index);
                groups.Insert(0, firstGroup);
            }

            return groups;
        }

        private List<IGrouping<string, MediaViewModel>> GetArtistGrouping(MusicLibraryFetchResult fetchResult)
        {
            var groups = Enumerable.GroupBy<MediaViewModel, string>(Songs, m => m.MainArtist?.Name ?? fetchResult.UnknownArtist.Name)
                .OrderBy(g => g.Key)
                .ToList();

            var index = groups.FindIndex(g => g.Key == fetchResult.UnknownArtist.Name);
            if (index >= 0)
            {
                var firstGroup = groups[index];
                groups.RemoveAt(index);
                groups.Insert(0, firstGroup);
            }

            return groups;
        }

        private List<IGrouping<string, MediaViewModel>> GetYearGrouping()
        {
            var groups = Enumerable.GroupBy<MediaViewModel, string>(Songs,
                    m =>
                    m.MediaInfo.MusicProperties.Year > 0
                        ? m.MediaInfo.MusicProperties.Year.ToString()
                        : MediaGroupingHelpers.OtherGroupSymbol)
                .OrderByDescending(g => g.Key == MediaGroupingHelpers.OtherGroupSymbol ? 0 : uint.Parse(g.Key))
                .ToList();
            return groups;
        }

        private List<IGrouping<string, MediaViewModel>> GetDateAddedGrouping()
        {
            var groups = Enumerable.GroupBy<MediaViewModel, DateTime>(Songs, m => m.DateAdded.Date)
                .OrderByDescending(g => g.Key)
                .Select(g =>
                    new ListGrouping<string, MediaViewModel>(
                        g.Key == default ? MediaGroupingHelpers.OtherGroupSymbol : g.Key.ToString("d", CultureInfo.CurrentCulture), g))
                .OfType<IGrouping<string, MediaViewModel>>()
                .ToList();
            return groups;
        }

        private List<IGrouping<string, MediaViewModel>> GetDefaultGrouping()
        {
            var groups = Enumerable
                .GroupBy<MediaViewModel, string>(Songs, m => MediaGroupingHelpers.GetFirstLetterGroup(m.Name))
                .ToList();

            var sortedGroup = new List<IGrouping<string, MediaViewModel>>();
            foreach (char header in MediaGroupingHelpers.GroupHeaders)
            {
                string groupHeader = header.ToString();
                if (groups.Find(g => g.Key == groupHeader) is { } group)
                {
                    sortedGroup.Add(group);
                }
                else
                {
                    sortedGroup.Add(new ListGrouping<string, MediaViewModel>(groupHeader));
                }
            }

            return sortedGroup;
        }

        private List<IGrouping<string, MediaViewModel>> GetCurrentGrouping(MusicLibraryFetchResult musicLibrary)
        {
            return SortBy switch
            {
                "album" => GetAlbumGrouping(musicLibrary),
                "artist" => GetArtistGrouping(musicLibrary),
                "year" => GetYearGrouping(),
                "dateAdded" => GetDateAddedGrouping(),
                _ => GetDefaultGrouping()
            };
        }

        private void OnMusicLibraryContentChanged(ILibraryService sender, object args)
        {
            dispatcherQueue.TryEnqueue(FetchSongs);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Screenbox.Core.ViewModels.SongsPageViewModel.SortBy))
            {
                var groups = GetCurrentGrouping(libraryService.GetMusicFetchResult());
                GroupedSongs.Clear();
                foreach (IGrouping<string, MediaViewModel> group in groups)
                {
                    GroupedSongs.AddGroup(group);
                }
            }
        }

        [RelayCommand]
        private void SetSortBy(string tag)
        {
            SortBy = tag;
        }

        [RelayCommand]
        private void Play(MediaViewModel media)
        {
            if (Songs.Count == 0) return;
            Messenger.SendQueueAndPlay(media, Songs);
        }
    }
}
