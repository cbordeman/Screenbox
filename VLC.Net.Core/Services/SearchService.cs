using System.Collections.Immutable;
using VLC.Net.Core.Models;
using VLC.Net.Core.ViewModels;
using MediaViewModel = VLC.Net.Core.ViewModels.MediaViewModel;

namespace VLC.Net.Core.Services
{
    public sealed class SearchService : ISearchService
    {
        private readonly ILibraryService libraryService;

        public SearchService(ILibraryService libraryService)
        {
            this.libraryService = libraryService;
        }

        public SearchResult SearchLocalLibrary(string query)
        {
            MusicLibraryFetchResult musicLibrary = libraryService.GetMusicFetchResult();
            IReadOnlyList<MediaViewModel> videosLibrary = libraryService.GetVideosFetchResult();

            ImmutableList<MediaViewModel> songs = musicLibrary.Songs
                .Select<MediaViewModel, (MediaViewModel Song, int Index)>(m => (Song: m, Index: m.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase)))
                .Where(t => t.Index >= 0)
                .OrderBy(t => t.Index)
                .Select(t => t.Song)
                .ToImmutableList();
            ImmutableList<AlbumViewModel> albums = musicLibrary.Albums
                .Select(a => (Album: a, Index: a.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase)))
                .Where(t => t.Index >= 0)
                .OrderBy(t => t.Index)
                .Select(t => t.Album)
                .ToImmutableList();
            ImmutableList<ArtistViewModel> artists = musicLibrary.Artists
                .Select(a => (Artist: a, Index: a.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase)))
                .Where(t => t.Index >= 0)
                .OrderBy(t => t.Index)
                .Select(t => t.Artist)
                .ToImmutableList();
            ImmutableList<MediaViewModel> videos = videosLibrary
                .Select<MediaViewModel, (MediaViewModel Video, int Index)>(m => (Video: m, Index: m.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase)))
                .Where(t => t.Index >= 0)
                .OrderBy(t => t.Index)
                .Select(t => t.Video)
                .ToImmutableList();

            return new SearchResult(query, songs, videos, artists, albums);
        }
    }
}
