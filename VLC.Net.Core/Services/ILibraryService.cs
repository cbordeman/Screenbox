using VLC.Net.Core.Models;
using MediaViewModel = VLC.Net.Core.ViewModels.MediaViewModel;

namespace VLC.Net.Core.Services
{
    public interface ILibraryService
    {
        event TypedEventHandler<ILibraryService, object>? MusicLibraryContentChanged;
        event TypedEventHandler<ILibraryService, object>? VideosLibraryContentChanged;
        string? MusicLibrary { get; }
        string? VideosLibrary { get; }
        public bool IsLoadingVideos { get; }
        public bool IsLoadingMusic { get; }
        Task<string?> InitializeMusicLibraryAsync();
        Task<string?> InitializeVideosLibraryAsync();
        Task FetchMusicAsync(bool useCache = true);
        Task FetchVideosAsync(bool useCache = true);
        MusicLibraryFetchResult GetMusicFetchResult();
        IReadOnlyList<MediaViewModel> GetVideosFetchResult();
    }
}