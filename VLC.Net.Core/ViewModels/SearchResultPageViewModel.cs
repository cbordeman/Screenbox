#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using VLC.Net.Core.Messages;
using VLC.Net.Core.Models;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class SearchResultPageViewModel : ObservableRecipient
    {
        public string SearchQuery { get; private set; }

        public SearchResult? SearchResult { get; private set; }

        public ObservableCollection<ArtistViewModel> Artists { get; }
        public ObservableCollection<AlbumViewModel> Albums { get; }
        public ObservableCollection<MediaViewModel> Songs { get; }
        public ObservableCollection<MediaViewModel> Videos { get; }

        [ObservableProperty] private bool showArtists;
        [ObservableProperty] private bool showAlbums;
        [ObservableProperty] private bool showSongs;
        [ObservableProperty] private bool showVideos;
        [ObservableProperty] private bool hasMoreArtists;
        [ObservableProperty] private bool hasMoreAlbums;
        [ObservableProperty] private bool hasMoreSongs;
        [ObservableProperty] private bool hasMoreVideos;

        private readonly INavigationService navigationService;

        public SearchResultPageViewModel(INavigationService navigationService)
        {
            this.navigationService = navigationService;
            SearchQuery = string.Empty;
            Artists = new ObservableCollection<ArtistViewModel>();
            Albums = new ObservableCollection<AlbumViewModel>();
            Songs = new ObservableCollection<MediaViewModel>();
            Videos = new ObservableCollection<MediaViewModel>();
        }

        public void Load(SearchResult searchResult)
        {
            SearchResult = searchResult;
            SearchQuery = searchResult.Query;
            if (searchResult.Artists.Count > 0)
            {
                ShowArtists = true;
            }

            if (searchResult.Albums.Count > 0)
            {
                ShowAlbums = true;
            }

            if (searchResult.Songs.Count > 0)
            {
                ShowSongs = true;
                foreach (MediaViewModel song in searchResult.Songs.Take(5))
                {
                    Songs.Add(song);
                }
            }

            if (searchResult.Videos.Count > 0)
            {
                ShowVideos = true;
                foreach (MediaViewModel video in searchResult.Videos.Take(6))
                {
                    Videos.Add(video);
                }
            }

            UpdateHasMoreProperties(searchResult);
        }

        public void UpdateGridItems(int requestedCount)
        {
            if (SearchResult == null) return;
            SyncCollection(Artists, SearchResult.Artists, requestedCount);
            SyncCollection(Albums, SearchResult.Albums, requestedCount);
            UpdateHasMoreProperties(SearchResult);
        }

        private void UpdateHasMoreProperties(SearchResult searchResult)
        {
            HasMoreArtists = Artists.Count < searchResult.Artists.Count;
            HasMoreAlbums = Albums.Count < searchResult.Albums.Count;
            HasMoreSongs = Songs.Count < searchResult.Songs.Count;
            HasMoreVideos = Videos.Count < searchResult.Videos.Count;
        }

        [RelayCommand]
        private void Play(MediaViewModel media)
        {
            Messenger.Send(new PlayMediaMessage(media));
        }

        [RelayCommand]
        private void PlaySong(MediaViewModel media)
        {
            if (SearchResult == null) return;
            Messenger.SendQueueAndPlay(media, SearchResult.Songs);
        }

        [RelayCommand(CanExecute = nameof(Screenbox.Core.ViewModels.SearchResultPageViewModel.HasMoreArtists))]
        private void SeeAllArtists()
        {
            navigationService.Navigate(typeof(ArtistSearchResultPageViewModel), this);
        }

        [RelayCommand(CanExecute = nameof(Screenbox.Core.ViewModels.SearchResultPageViewModel.HasMoreAlbums))]
        private void SeeAllAlbums()
        {
            navigationService.Navigate(typeof(AlbumSearchResultPageViewModel), this);
        }

        [RelayCommand(CanExecute = nameof(Screenbox.Core.ViewModels.SearchResultPageViewModel.HasMoreSongs))]
        private void SeeAllSongs()
        {
            navigationService.Navigate(typeof(SongSearchResultPageViewModel), this);
        }

        [RelayCommand(CanExecute = nameof(Screenbox.Core.ViewModels.SearchResultPageViewModel.HasMoreVideos))]
        private void SeeAllVideos()
        {
            navigationService.Navigate(typeof(VideoSearchResultPageViewModel), this);
        }

        private static void SyncCollection<T>(IList<T> target, IReadOnlyList<T> source, int desiredCount)
        {
            desiredCount = Math.Min(desiredCount, source.Count);
            if (desiredCount <= 0)
            {
                target.Clear();
                return;
            }

            if (target.Count > desiredCount)
            {
                int countToRemove = target.Count - desiredCount;
                for (int i = 0; i < countToRemove; i++)
                {
                    target.RemoveAt(target.Count - 1);
                }
            }
            else
            {
                for (int i = target.Count; i < desiredCount; i++)
                {
                    target.Add(source[i]);
                }
            }
        }
    }
}
