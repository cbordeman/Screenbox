#nullable enable

using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LibVLCSharp.Shared;
using VLC.Net.Core.Common;
using VLC.Net.Core.Enums;
using VLC.Net.Core.Factories;
using VLC.Net.Core.Messages;
using VLC.Net.Core.Models;
using VLC.Net.Core.Playback;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    public partial class MediaViewModel : ObservableRecipient
    {
        public string Location { get; }

        public object Source { get; private set; }

        public bool IsFromLibrary { get; set; }

        public ArtistViewModel? MainArtist => Enumerable.FirstOrDefault<ArtistViewModel>(Artists);

        public Lazy<PlaybackItem?> Item { get; internal set; }

        public IReadOnlyList<string> Options { get; }

        public DateTimeOffset DateAdded { get; set; }

        public MediaPlaybackType MediaType => MediaInfo.MediaType;

        public TimeSpan Duration => MediaInfo.MusicProperties.Duration > TimeSpan.Zero
            ? MediaInfo.MusicProperties.Duration
            : MediaInfo.VideoProperties.Duration;

        public string DurationText => Duration > TimeSpan.Zero ? Humanizer.ToDuration(Duration) : string.Empty;     // Helper for binding

        public string TrackNumberText =>
            MediaInfo.MusicProperties.TrackNumber > 0 ? MediaInfo.MusicProperties.TrackNumber.ToString() : string.Empty;    // Helper for binding

        public Bitmap? Thumbnail
        {
            get
            {
                if (thumbnailRef == null) return null;
                return thumbnailRef.TryGetTarget(out var image) ? image : null;
            }
            set
            {
                if (thumbnailRef == null && value == null) return;
                if ((thumbnailRef?.TryGetTarget(out var image) ?? false) && image == value) return;
                SetProperty(ref thumbnailRef, value == null ? null : new WeakReference<Bitmap>(value));
            }
        }

        private readonly LibVlcService libVlcService;
        private readonly List<string> options;

        [ObservableProperty] private string name;
        [ObservableProperty] private bool isMediaActive;
        [ObservableProperty] private bool isAvailable = true;
        [ObservableProperty] private AlbumViewModel? album;
        [ObservableProperty] private string? caption;  // For list item subtitle
        [ObservableProperty] private string? altCaption;   // For player page subtitle

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DurationText))]
        [NotifyPropertyChangedFor(nameof(TrackNumberText))]
        private MediaInfo mediaInfo;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MainArtist))]
        private ArtistViewModel[] artists;

        [ObservableProperty]
        private bool? isPlaying;

        private WeakReference<Bitmap>? thumbnailRef;

        public MediaViewModel(MediaViewModel source)
        {
            libVlcService = source.libVlcService;
            name = source.name;
            thumbnailRef = source.thumbnailRef;
            mediaInfo = source.mediaInfo;
            artists = source.artists;
            album = source.album;
            caption = source.caption;
            altCaption = source.altCaption;
            options = new List<string>(source.Options);
            Options = new ReadOnlyCollection<string>(options);
            Location = source.Location;
            Source = source.Source;
            Item = source.Item;
            DateAdded = source.DateAdded;
        }

        private MediaViewModel(object source, MediaInfo mediaInfo, LibVlcService libVlcService)
        {
            this.libVlcService = libVlcService;
            Source = source;
            Location = string.Empty;
            DateAdded = DateTimeOffset.Now;
            name = string.Empty;
            this.mediaInfo = mediaInfo;
            artists = Array.Empty<ArtistViewModel>();
            options = new List<string>();
            Options = new ReadOnlyCollection<string>(options);
            Item = new Lazy<PlaybackItem?>(CreatePlaybackItem);
        }

        public MediaViewModel(LibVlcService libVlcService, IStorageFile file)
            : this(file, new MediaInfo(FilesHelpers.GetMediaTypeForFile(file)), libVlcService)
        {
            Location = file.Path.GetFilePath();;
            name = file.DisplayName;
            altCaption = file.Name;
        }

        public MediaViewModel(LibVlcService libVlcService, Uri uri)
            : this(uri, new MediaInfo(MediaPlaybackType.Unknown), libVlcService)
        {
            Guard.IsTrue(uri.IsAbsoluteUri);
            Location = uri.OriginalString;
            name = uri.Segments.Length > 0 ? Uri.UnescapeDataString(uri.Segments.Last()) : string.Empty;
        }

        public MediaViewModel(LibVlcService libVlcService, Media media)
            : this(media, new MediaInfo(MediaPlaybackType.Unknown), libVlcService)
        {
            Location = media.Mrl;

            // Media is already loaded, create PlaybackItem
            Item = new Lazy<PlaybackItem?>(new PlaybackItem(media, media));
        }

        partial void OnMediaInfoChanged(MediaInfo value)
        {
            UpdateCaptions();
        }

        private PlaybackItem? CreatePlaybackItem()
        {
            PlaybackItem? item = null;
            try
            {
                if (Source is Media mediaSource)
                {
                    item = new PlaybackItem(mediaSource, mediaSource);
                }
                else
                {
                    Media media = libVlcService.CreateMedia(Source, options.ToArray());
                    item = new PlaybackItem(Source, media);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // Coding error. Rethrow.
                throw;
            }
            catch (Exception e)
            {
                Messenger.Send(new MediaLoadFailedNotificationMessage(e.Message, Location));
            }

            return item;
        }

        public void SetOptions(string options)
        {
            string[] opts = options.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(o => o.StartsWith(":") && o.Length > 1).ToArray();

            // Check if new options and existing options are the same
            if (opts.Length == this.options.Count)
            {
                bool same = !opts.Where((o, i) => o != this.options[i]).Any();
                if (same) return;
            }

            this.options.Clear();
            this.options.AddRange(opts);

            if (!Item.IsValueCreated) return;
            Clean();
        }

        public void Clean()
        {
            // If source is Media then there is no way to recreate. Don't clean up.
            if (Source is Media || !Item.IsValueCreated) return;
            PlaybackItem? item = Item.Value;
            Item = new Lazy<PlaybackItem?>(CreatePlaybackItem);
            if (item == null) return;
            libVlcService.DisposeMedia(item.Media);
        }

        public void UpdateSource(IStorageFile file)
        {
            Source = file;
            Name = file.GetDisplayName();
            AltCaption = file.Name;
        }

        public async Task LoadDetailsAsync(IFilesService filesService)
        {
            switch (Source)
            {
                case IStorageFile file:
                    MediaInfo = await filesService.GetMediaInfoAsync(file);
                    break;
                case Uri uri when await TryGetStorageFileFromUri(uri) is { } uriFile:
                    UpdateSource(uriFile);
                    MediaInfo = await filesService.GetMediaInfoAsync(uriFile);
                    break;
            }

            switch (MediaType)
            {
                case MediaPlaybackType.Unknown when Item is { IsValueCreated: true, Value: { VideoTracks.Count: 0, Media.ParsedStatus: MediaParsedStatus.Done } }:
                    // Update media type when it was previously set Unknown. Usually when source is a URI.
                    // We don't want to init PlaybackItem just for this.
                    MediaInfo.MediaType = MediaPlaybackType.Music;
                    break;
                case MediaPlaybackType.Music when !string.IsNullOrEmpty(MediaInfo.MusicProperties.Title):
                    Name = MediaInfo.MusicProperties.Title;
                    break;
                case MediaPlaybackType.Video when !string.IsNullOrEmpty(MediaInfo.VideoProperties.Title):
                    Name = MediaInfo.VideoProperties.Title;
                    break;
            }

            if (Item is { IsValueCreated: true, Value.Media: { IsParsed: true } media })
            {
                if (Source is not IStorageItem &&
                    media.Meta(MetadataType.Title) is { } title &&
                    !string.IsNullOrEmpty(title) &&
                    !Guid.TryParse(title, out Guid _))
                {
                    Name = title;
                }

                VideoInfo videoProperties = MediaInfo.VideoProperties;
                videoProperties.ShowName = media.Meta(MetadataType.ShowName) ?? videoProperties.ShowName;
                videoProperties.Season = media.Meta(MetadataType.Season) ?? videoProperties.Season;
                videoProperties.Episode = media.Meta(MetadataType.Episode) ?? videoProperties.Episode;
            }
        }

        public async Task LoadThumbnailAsync()
        {
            if (Thumbnail != null) return;
            if (Source is Uri uri && await TryGetStorageFileFromUri(uri) is { } storageFile)
            {
                UpdateSource(storageFile);
            }

            if (Source is IStorageFile file)
            {
                var bitmap = new Bitmap(file.Path.GetFilePath())
                Bitmap scaled = bitmap.CreateScaledBitmap(
                    new PixelSize(bitmap., 300),
                    BitmapInterpolationMode.HighQuality);
                Thumbnail = image;
            }
            else if (Item is { IsValueCreated: true, Value.Media: { } media } &&
                     media.Meta(MetadataType.ArtworkURL) is { } artworkUrl &&
                     Uri.TryCreate(artworkUrl, UriKind.Absolute, out Uri artworkUri))
            {
                Thumbnail = new BitmapImage(artworkUri)
                {
                    DecodePixelType = DecodePixelType.Logical,
                    DecodePixelHeight = 300
                };
            }
        }

        public Task<IRandomAccessStream?> GetThumbnailSourceAsync()
        {
            return Source is not StorageFile file
                ? Task.FromResult<IRandomAccessStream?>(null)
                : GetThumbnailSourceAsync(file);
        }

        private static async Task<IRandomAccessStream?> GetThumbnailSourceAsync(StorageFile file)
        {
            return await GetCoverFromTagAsync(file) ?? await GetStorageFileThumbnailAsync(file);
        }

        private static async Task<IRandomAccessStream?> GetCoverFromTagAsync(StorageFile file)
        {
            if (!file.IsAvailable) return null;
            try
            {
                using var stream = await file.OpenStreamForReadAsync(); // Throwable: FileNotFoundException
                var name = string.IsNullOrEmpty(file.Path) ? file.Name : file.Path;
                var fileAbstract = new StreamAbstraction(name, stream);
                using var tagFile = TagLib.File.Create(fileAbstract, ReadStyle.PictureLazy);
                if (tagFile.Tag.Pictures.Length == 0) return null;
                var cover =
                    tagFile.Tag.Pictures.FirstOrDefault(p => p.Type is PictureType.FrontCover or PictureType.Media) ??
                    tagFile.Tag.Pictures.FirstOrDefault(p => p.Type != PictureType.NotAPicture);
                if (cover == null) return null;
                if (cover.Data.IsEmpty)
                {
                    if (cover is not ILazy or ILazy { IsLoaded: true }) return null;
                    ((ILazy)cover).Load();
                }

                var inMemoryStream = new InMemoryRandomAccessStream();
                await inMemoryStream.WriteAsync(cover.Data.Data.AsBuffer());
                inMemoryStream.Seek(0);
                return inMemoryStream;
            }
            catch (Exception)
            {
                // FileNotFoundException
                // UnsupportedFormatException
                // CorruptFileException
                // pass
            }

            return null;
        }

        private static async Task<IRandomAccessStream?> GetStorageFileThumbnailAsync(StorageFile file)
        {
            if (!file.IsAvailable) return null;
            try
            {
                StorageItemThumbnail? source = await file.GetThumbnailAsync(ThumbnailMode.SingleItem);
                if (source is { Type: ThumbnailType.Image })
                {
                    return source;
                }
            }
            catch (Exception)
            {
                //// System.Exception: The data necessary to complete this operation is not yet available.
                //if (e.HResult != unchecked((int)0x8000000A) &&
                //    // System.Exception: The RPC server is unavailable.
                //    e.HResult != unchecked((int)0x800706BA))
                //    LogService.Log(e);
            }

            return null;
        }

        public void UpdateAlbum(AlbumViewModelFactory factory)
        {
            if (!IsFromLibrary || MediaType != MediaPlaybackType.Music) return;
            MusicInfo musicProperties = MediaInfo.MusicProperties;
            if (Album != null)
            {
                if (factory.GetAlbumFromName(musicProperties.Album, musicProperties.AlbumArtist) == Album)
                    return;

                factory.Remove(this);
            }

            Album = factory.AddSongToAlbum(this, musicProperties.Album, musicProperties.AlbumArtist, musicProperties.Year);
        }

        public void UpdateArtists(ArtistViewModelFactory factory)
        {
            if (!IsFromLibrary || MediaType != MediaPlaybackType.Music) return;
            if (Artists.Length > 0)
            {
                ArtistViewModel[] artists = factory.ParseArtists(MediaInfo.MusicProperties.Artist);
                if (MemoryExtensions.SequenceEqual(artists, Artists)) return;
                factory.Remove(this);
            }

            Artists = factory.ParseAddArtists(MediaInfo.MusicProperties.Artist, this);
        }

        private void UpdateCaptions()
        {
            if (Duration > TimeSpan.Zero)
            {
                Caption = Humanizer.ToDuration(Duration);
            }

            MusicInfo musicProperties = MediaInfo.MusicProperties;
            if (!string.IsNullOrEmpty(musicProperties.Artist))
            {
                Caption = musicProperties.Artist;
                AltCaption = string.IsNullOrEmpty(musicProperties.Album)
                    ? musicProperties.Artist
                    : $"{musicProperties.Artist} – {musicProperties.Album}";
            }
            else if (!string.IsNullOrEmpty(musicProperties.Album))
            {
                AltCaption = musicProperties.Album;
            }

            if (Item is { IsValueCreated: true, Value.Media: { IsParsed: true } media })
            {
                string artist = media.Meta(MetadataType.Artist) ?? string.Empty;
                if (!string.IsNullOrEmpty(artist))
                {
                    Caption = artist;
                }

                if (media.Meta(MetadataType.Album) is { } album && !string.IsNullOrEmpty(album))
                {
                    AltCaption = string.IsNullOrEmpty(artist) ? album : $"{artist} – {album}";
                }
            }
        }

        private static async Task<StorageFile?> TryGetStorageFileFromUri(Uri uri)
        {
            if (uri is { IsFile: true, IsLoopback: true, IsAbsoluteUri: true })
            {
                try
                {
                    return await StorageFile.GetFileFromPathAsync(uri.OriginalString);
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return null;
        }
    }
}