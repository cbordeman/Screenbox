using VLC.Net.Core.Factories;
using VLC.Net.Core.Helpers;
using VLC.Net.Core.Services;
using VLC.Net.Core.ViewModels;
using AlbumDetailsPageViewModel = VLC.Net.Core.ViewModels.AlbumDetailsPageViewModel;
using AlbumsPageViewModel = VLC.Net.Core.ViewModels.AlbumsPageViewModel;
using AllVideosPageViewModel = VLC.Net.Core.ViewModels.AllVideosPageViewModel;
using ArtistDetailsPageViewModel = VLC.Net.Core.ViewModels.ArtistDetailsPageViewModel;
using CastControlViewModel = VLC.Net.Core.ViewModels.CastControlViewModel;
using ChapterViewModel = VLC.Net.Core.ViewModels.ChapterViewModel;
using CommonViewModel = VLC.Net.Core.ViewModels.CommonViewModel;
using CompositeTrackPickerViewModel = VLC.Net.Core.ViewModels.CompositeTrackPickerViewModel;
using FolderViewPageViewModel = VLC.Net.Core.ViewModels.FolderViewPageViewModel;
using HomePageViewModel = VLC.Net.Core.ViewModels.HomePageViewModel;
using LivelyWallpaperPlayerViewModel = VLC.Net.Core.ViewModels.LivelyWallpaperPlayerViewModel;
using LivelyWallpaperSelectorViewModel = VLC.Net.Core.ViewModels.LivelyWallpaperSelectorViewModel;
using MainPageViewModel = VLC.Net.Core.ViewModels.MainPageViewModel;
using MediaListViewModel = VLC.Net.Core.ViewModels.MediaListViewModel;
using MusicPageViewModel = VLC.Net.Core.ViewModels.MusicPageViewModel;
using NetworkPageViewModel = VLC.Net.Core.ViewModels.NetworkPageViewModel;
using NotificationViewModel = VLC.Net.Core.ViewModels.NotificationViewModel;
using PlayerControlsViewModel = VLC.Net.Core.ViewModels.PlayerControlsViewModel;
using PlayerPageViewModel = VLC.Net.Core.ViewModels.PlayerPageViewModel;
using PlaylistViewModel = VLC.Net.Core.ViewModels.PlaylistViewModel;
using PlayQueuePageViewModel = VLC.Net.Core.ViewModels.PlayQueuePageViewModel;
using PropertyViewModel = VLC.Net.Core.ViewModels.PropertyViewModel;
using SearchResultPageViewModel = VLC.Net.Core.ViewModels.SearchResultPageViewModel;
using SeekBarViewModel = VLC.Net.Core.ViewModels.SeekBarViewModel;
using SettingsPageViewModel = VLC.Net.Core.ViewModels.SettingsPageViewModel;
using SongsPageViewModel = VLC.Net.Core.ViewModels.SongsPageViewModel;
using VideosPageViewModel = VLC.Net.Core.ViewModels.VideosPageViewModel;
using VolumeViewModel = VLC.Net.Core.ViewModels.VolumeViewModel;

namespace VLC.Net.Core.Common;

public static class ServiceHelpers
{
    public static void PopulateCoreRegistrations()
    {
        // View models
        SplatRegistrations.RegisterLazySingleton<PlayerElementViewModel>();
        SplatRegistrations.RegisterLazySingleton<PropertyViewModel>();
        SplatRegistrations.RegisterLazySingleton<ChapterViewModel>();
        SplatRegistrations.RegisterLazySingleton<CompositeTrackPickerViewModel>();
        SplatRegistrations.RegisterLazySingleton<SeekBarViewModel>();
        SplatRegistrations.RegisterLazySingleton<VideosPageViewModel>();
        SplatRegistrations.RegisterLazySingleton<NetworkPageViewModel>();
        SplatRegistrations.RegisterLazySingleton<FolderViewPageViewModel>();
        SplatRegistrations.RegisterLazySingleton<FolderListViewPageViewModel>();
        SplatRegistrations.RegisterLazySingleton<PlayerControlsViewModel>();
        SplatRegistrations.RegisterLazySingleton<CastControlViewModel>();
        SplatRegistrations.RegisterLazySingleton<PlayerPageViewModel>();
        SplatRegistrations.RegisterLazySingleton<MainPageViewModel>();
        SplatRegistrations.RegisterLazySingleton<PlayQueuePageViewModel>();
        SplatRegistrations.RegisterLazySingleton<SettingsPageViewModel>();
        SplatRegistrations.RegisterLazySingleton<PlaylistViewModel>();
        SplatRegistrations.RegisterLazySingleton<AlbumDetailsPageViewModel>();
        SplatRegistrations.RegisterLazySingleton<ArtistDetailsPageViewModel>();
        SplatRegistrations.RegisterLazySingleton<SongsPageViewModel>();
        SplatRegistrations.RegisterLazySingleton<AlbumsPageViewModel>();
        SplatRegistrations.RegisterLazySingleton<ArtistsPageViewModel>();
        SplatRegistrations.RegisterLazySingleton<AllVideosPageViewModel>();
        SplatRegistrations.RegisterLazySingleton<MusicPageViewModel>();
        SplatRegistrations.RegisterLazySingleton<SearchResultPageViewModel>();
        SplatRegistrations.RegisterLazySingleton<NotificationViewModel>();
        SplatRegistrations.RegisterLazySingleton<LivelyWallpaperPlayerViewModel>();
        SplatRegistrations.RegisterLazySingleton<LivelyWallpaperSelectorViewModel>();
        SplatRegistrations.RegisterLazySingleton<HomePageViewModel>();
        // Shared between many pages
        SplatRegistrations.RegisterLazySingleton<CommonViewModel>();
        // Avoid thread lock
        SplatRegistrations.RegisterLazySingleton<VolumeViewModel>();   
        // Global playlist
        SplatRegistrations.RegisterLazySingleton<MediaListViewModel>();

        // Misc
        SplatRegistrations.RegisterLazySingleton<LastPositionTracker>();

        // Factories
        SplatRegistrations.RegisterLazySingleton<MediaViewModelFactory>();
        SplatRegistrations.RegisterLazySingleton<StorageItemViewModelFactory>();
        SplatRegistrations.RegisterLazySingleton<ArtistViewModelFactory>();
        SplatRegistrations.RegisterLazySingleton<AlbumViewModelFactory>();

        // SplatRegistrations.RegisterLazySingleton
        SplatRegistrations.RegisterLazySingleton<LibVlcService>();
        SplatRegistrations.RegisterLazySingleton<IFilesService, FilesService>();
        SplatRegistrations.RegisterLazySingleton<ILibraryService, LibraryService>();
        SplatRegistrations.RegisterLazySingleton<ISearchService, SearchService>();
        SplatRegistrations.RegisterLazySingleton<INotificationService, NotificationService>();
        SplatRegistrations.RegisterLazySingleton<IWindowService, WindowService>();
        SplatRegistrations.RegisterLazySingleton<ICastService, CastService>();
        SplatRegistrations.RegisterLazySingleton<ISettingsService, SettingsService>();
        SplatRegistrations.RegisterLazySingleton<ISystemMediaTransportControlsService, SystemMediaTransportControlsService>();
        SplatRegistrations.RegisterLazySingleton<ILivelyWallpaperService, LivelyWallpaperService>();
    }
}