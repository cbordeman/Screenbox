#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using VLC.Net.Core.Common;
using VLC.Net.Core.Helpers;
using VLC.Net.Core.Messages;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class ArtistDetailsPageViewModel : ObservableRecipient
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalDuration))]
        [NotifyPropertyChangedFor(nameof(SongsCount))]
        private ArtistViewModel? source;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AlbumsCount))]
        private List<IGrouping<AlbumViewModel?, MediaViewModel>> albums;

        public TimeSpan TotalDuration => Source != null ? GetTotalDuration(Source.RelatedSongs) : TimeSpan.Zero;

        public int AlbumsCount => Albums.Count;

        public int SongsCount => Source?.RelatedSongs.Count ?? 0;

        private List<MediaViewModel>? itemList;

        public ArtistDetailsPageViewModel()
        {
            albums = new List<IGrouping<AlbumViewModel?, MediaViewModel>>();
        }

        public void OnNavigatedTo(object? parameter)
        {
            Source = parameter switch
            {
                NavigationMetadata { Parameter: ArtistViewModel source } => source,
                ArtistViewModel source => source,
                _ => throw new ArgumentException("Navigation parameter is not an artist")
            };
        }

        async partial void OnSourceChanged(ArtistViewModel? value)
        {
            if (value == null)
            {
                Albums = new List<IGrouping<AlbumViewModel?, MediaViewModel>>();
                return;
            }

            Albums = value.RelatedSongs
                .OrderBy<MediaViewModel, uint>(m => m.MediaInfo.MusicProperties.TrackNumber)
                .ThenBy(m => m.Name, StringComparer.CurrentCulture)
                .GroupBy<MediaViewModel, AlbumViewModel>(m => m.Album)
                .OrderByDescending(g => g.Key?.Year ?? 0).ToList();

            IEnumerable<Task> loadingTasks = Enumerable.Where<IGrouping<AlbumViewModel, MediaViewModel>>(Albums, g => g.Key is { AlbumArt: null })
                .Select(g => g.Key?.LoadAlbumArtAsync())
                .OfType<Task>();
            await Task.WhenAll(loadingTasks);
        }

        [RelayCommand]
        private void Play(MediaViewModel? media)
        {
            itemList ??= Enumerable.SelectMany<IGrouping<AlbumViewModel, MediaViewModel>, MediaViewModel>(Albums, g => g).ToList();
            Messenger.SendQueueAndPlay(media ?? itemList[0], itemList);
        }

        [RelayCommand]
        private void ShuffleAndPlay()
        {
            if (Source == null || Source.RelatedSongs.Count == 0) return;
            Random rnd = new();
            List<MediaViewModel> shuffledList = Enumerable.OrderBy<MediaViewModel, int>(Source.RelatedSongs, _ => rnd.Next()).ToList();
            Messenger.Send(new ClearPlaylistMessage());
            Messenger.Send(new QueuePlaylistMessage(shuffledList));
            Messenger.Send(new PlayMediaMessage(shuffledList[0], true));
        }

        private static TimeSpan GetTotalDuration(IEnumerable<MediaViewModel> items)
        {
            TimeSpan duration = TimeSpan.Zero;
            foreach (MediaViewModel item in items)
            {
                duration += item.Duration;
            }

            return duration;
        }
    }
}
