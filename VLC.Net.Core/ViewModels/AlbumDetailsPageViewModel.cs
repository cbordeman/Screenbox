#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using VLC.Net.Core.Common;
using VLC.Net.Core.Messages;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class AlbumDetailsPageViewModel : ObservableRecipient
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Year))]
        [NotifyPropertyChangedFor(nameof(SongsCount))]
        [NotifyPropertyChangedFor(nameof(TotalDuration))]
        private AlbumViewModel? source;

        public uint? Year => Source?.Year;

        public int SongsCount => Source?.RelatedSongs.Count ?? 0;

        public TimeSpan TotalDuration => Source != null ? GetTotalDuration(Source.RelatedSongs) : TimeSpan.Zero;

        public ObservableCollection<MediaViewModel> SortedItems { get; }

        private List<MediaViewModel>? itemList;

        public AlbumDetailsPageViewModel()
        {
            SortedItems = new ObservableCollection<MediaViewModel>();
        }

        public void OnNavigatedTo(object? parameter)
        {
            Source = parameter switch
            {
                NavigationMetadata { Parameter: AlbumViewModel source } => source,
                AlbumViewModel source => source,
                _ => throw new ArgumentException("Navigation parameter is not an album")
            };
        }

        async partial void OnSourceChanged(AlbumViewModel? value)
        {
            if (value == null)
            {
                SortedItems.Clear();
                itemList = null;
                return;
            }

            var sorted = value.RelatedSongs.OrderBy(m =>
                    m.MediaInfo.MusicProperties.TrackNumber != 0    // Track number should start with 1
                        ? m.MediaInfo.MusicProperties.TrackNumber
                        : uint.MaxValue)
                .ThenBy(m => m.Name, StringComparer.CurrentCulture);

            SortedItems.Clear();
            foreach (MediaViewModel media in sorted)
            {
                SortedItems.Add(media);
            }

            if (value.AlbumArt == null)
            {
                await value.LoadAlbumArtAsync();
            }
        }

        [RelayCommand]
        private void Play(MediaViewModel item)
        {
            itemList ??= SortedItems.ToList();
            Messenger.SendQueueAndPlay(item, itemList);
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
