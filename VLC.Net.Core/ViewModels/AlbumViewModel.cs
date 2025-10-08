#nullable enable

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using VLC.Net.Core.Messages;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class AlbumViewModel : ObservableRecipient
    {
        public string Name { get; }

        public string ArtistName => string.IsNullOrEmpty(albumArtist)
            ? songArtist
            : albumArtist;

        public uint? Year
        {
            get => year;
            set
            {
                if (value > 0)
                {
                    year = value;
                }
            }
        }

        public BitmapImage? AlbumArt => albumArt;

        public DateTimeOffset DateAdded { get; set; }

        public ObservableCollection<MediaViewModel> RelatedSongs { get; }

        [ObservableProperty] private bool isPlaying;

        private readonly string albumArtist;
        private string songArtist;
        private BitmapImage? albumArt;
        private uint? year;

        public AlbumViewModel(string album, string albumArtist)
        {
            Name = album;
            this.albumArtist = albumArtist;
            songArtist = string.Empty;
            RelatedSongs = new ObservableCollection<MediaViewModel>();
            RelatedSongs.CollectionChanged += RelatedSongsOnCollectionChanged;
        }

        public async Task LoadAlbumArtAsync()
        {
            if (RelatedSongs.Count > 0)
            {
                await RelatedSongs[0].LoadThumbnailAsync();
                UpdateProperties();
            }
        }

        public override string ToString()
        {
            return $"{Name};{ArtistName}";
        }

        private void RelatedSongsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (MediaViewModel media in e.OldItems.OfType<MediaViewModel>())
                {
                    media.PropertyChanged -= MediaOnPropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (MediaViewModel media in e.NewItems.OfType<MediaViewModel>())
                {
                    media.PropertyChanged += MediaOnPropertyChanged;
                }
            }

            UpdateProperties();
        }

        private void MediaOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Screenbox.Core.ViewModels.MediaViewModel.IsPlaying) && sender is MediaViewModel media)
            {
                IsPlaying = media.IsPlaying ?? false;
            }

            if (RelatedSongs.Count > 0 && ReferenceEquals(RelatedSongs[0], sender))
            {
                UpdateProperties();
            }
        }

        private void UpdateProperties()
        {
            if (RelatedSongs.Count == 0) return;
            MediaViewModel first = RelatedSongs[0];
            SetProperty(ref albumArt, first.Thumbnail, nameof(AlbumArt));
            SetProperty(ref songArtist, first.MainArtist?.Name ?? string.Empty, nameof(ArtistName));
        }

        [RelayCommand]
        private void PlayAlbum()
        {
            if (RelatedSongs.Count == 0) return;
            MediaViewModel? inQueue = RelatedSongs.FirstOrDefault(m => m.IsMediaActive);
            if (inQueue != null)
            {
                Messenger.Send(new TogglePlayPauseMessage(false));
            }
            else
            {
                List<MediaViewModel> songs = RelatedSongs
                .OrderBy<MediaViewModel, uint>(m => m.MediaInfo.MusicProperties.TrackNumber)
                    .ThenBy(m => m.Name, StringComparer.CurrentCulture)
                    .ToList();

                Messenger.SendQueueAndPlay(inQueue ?? songs[0], songs);
            }
        }
    }
}
