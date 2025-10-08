using Avalonia.Threading;
using CommunityToolkit.Mvvm.Collections;
using VLC.Net.Core.Models;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    public sealed class ArtistsPageViewModel : BaseMusicContentViewModel
    {
        public ObservableGroupedCollection<string, ArtistViewModel> GroupedArtists { get; }

        private readonly ILibraryService libraryService;
        private readonly Dispatcher dispatcherQueue;
        private readonly DispatcherTimer refreshTimer;

        public ArtistsPageViewModel(ILibraryService libraryService)
        {
            this.libraryService = libraryService;
            dispatcherQueue = Dispatcher.UIThread;
            refreshTimer = new DispatcherTimer();
            GroupedArtists = [];
            PopulateGroups();

            libraryService.MusicLibraryContentChanged += OnMusicLibraryContentChanged;
        }

        public void OnNavigatedFrom()
        {
            libraryService.MusicLibraryContentChanged -= OnMusicLibraryContentChanged;
            refreshTimer.Stop();
        }

        public void FetchArtists()
        {
            // No need to run fetch async. HomePageViewModel should already be called the method.
            var musicLibrary = libraryService.GetMusicFetchResult();
            Songs = musicLibrary.Songs;

            var groupings = GetDefaultGrouping(musicLibrary);
            GroupedArtists.SyncObservableGroups(groupings);

            // Progressively update when it's still loading
            if (libraryService.IsLoadingMusic)
                refreshTimer.Debounce(FetchArtists, TimeSpan.FromSeconds(5));
            else
                refreshTimer.Stop();
        }

        private List<IGrouping<string, ArtistViewModel>> 
            GetDefaultGrouping(MusicLibraryFetchResult fetchResult)
        {
            var groups = fetchResult.Artists
                .OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
                .GroupBy(artist => artist == fetchResult.UnknownArtist
                    ? MediaGroupingHelpers.OtherGroupSymbol
                    : MediaGroupingHelpers.GetFirstLetterGroup(artist.Name))
                .ToList();

            var sortedGroup = new List<IGrouping<string, ArtistViewModel>>();
            foreach (char header in MediaGroupingHelpers.GroupHeaders)
            {
                string groupHeader = header.ToString();
                if (groups.Find(g => g.Key == groupHeader) is { } group)
                    sortedGroup.Add(group);
                else
                    sortedGroup.Add(new ListGrouping<string, ArtistViewModel>(groupHeader));
            }

            return sortedGroup;
        }

        private void PopulateGroups()
        {
            foreach (string key in MediaGroupingHelpers.GroupHeaders.Select(letter => letter.ToString()))
                GroupedArtists.AddGroup(key);
        }

        private void OnMusicLibraryContentChanged(ILibraryService sender, object args)
        {
            dispatcherQueue.Post(FetchArtists);
        }
    }
}