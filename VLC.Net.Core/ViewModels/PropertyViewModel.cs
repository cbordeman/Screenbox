#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VLC.Net.Core.Common;
using VLC.Net.Core.Enums;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class PropertyViewModel : ObservableObject
    {
        public Dictionary<string, string> MediaProperties { get; }

        public Dictionary<string, string> VideoProperties { get; }

        public Dictionary<string, string> AudioProperties { get; }

        public Dictionary<string, string> FileProperties { get; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(Screenbox.Core.ViewModels.PropertyViewModel.OpenFileLocationCommand))]
        private bool canNavigateToFile;

        private readonly IFilesService filesService;
        private readonly IResourceService resourceService;
        private StorageFile? mediaFile;
        private Uri? mediaUri;

        public PropertyViewModel(IFilesService filesService, IResourceService resourceService)
        {
            this.filesService = filesService;
            this.resourceService = resourceService;
            MediaProperties = new Dictionary<string, string>();
            VideoProperties = new Dictionary<string, string>();
            AudioProperties = new Dictionary<string, string>();
            FileProperties = new Dictionary<string, string>();
        }

        public async void OnLoaded(MediaViewModel media)
        {
            await media.LoadDetailsAsync(filesService);
        }

        public void UpdateProperties(MediaViewModel media)
        {
            switch (media.MediaType)
            {
                case MediaPlaybackType.Video:
                    MediaProperties[resourceService.GetString(ResourceName.PropertyTitle)] = string.IsNullOrEmpty(media.MediaInfo.VideoProperties.Title)
                        ? media.Name
                        : media.MediaInfo.VideoProperties.Title;
                    MediaProperties[resourceService.GetString(ResourceName.PropertySubtitle)] = media.MediaInfo.VideoProperties.Subtitle;
                    MediaProperties[resourceService.GetString(ResourceName.PropertyYear)] = media.MediaInfo.VideoProperties.Year > 0
                        ? media.MediaInfo.VideoProperties.Year.ToString()
                        : string.Empty;
                    MediaProperties[resourceService.GetString(ResourceName.PropertyProducers)] = string.Join("; ", media.MediaInfo.VideoProperties.Producers);
                    MediaProperties[resourceService.GetString(ResourceName.PropertyWriters)] = string.Join("; ", media.MediaInfo.VideoProperties.Writers);
                    MediaProperties[resourceService.GetString(ResourceName.PropertyLength)] = Humanizer.ToDuration((TimeSpan)media.MediaInfo.VideoProperties.Duration);

                    VideoProperties[resourceService.GetString(ResourceName.PropertyResolution)] = $"{media.MediaInfo.VideoProperties.Width}×{media.MediaInfo.VideoProperties.Height}";
                    VideoProperties[resourceService.GetString(ResourceName.PropertyBitRate)] = $"{media.MediaInfo.VideoProperties.Bitrate / 1000} kbps";

                    AudioProperties[resourceService.GetString(ResourceName.PropertyBitRate)] = $"{media.MediaInfo.MusicProperties.Bitrate / 1000} kbps";
                    break;

                case MediaPlaybackType.Music:
                    MediaProperties[resourceService.GetString(ResourceName.PropertyTitle)] = media.MediaInfo.MusicProperties.Title;
                    MediaProperties[resourceService.GetString(ResourceName.PropertyContributingArtists)] = media.MediaInfo.MusicProperties.Artist;
                    MediaProperties[resourceService.GetString(ResourceName.PropertyAlbum)] = media.MediaInfo.MusicProperties.Album;
                    MediaProperties[resourceService.GetString(ResourceName.PropertyAlbumArtist)] = media.MediaInfo.MusicProperties.AlbumArtist;
                    MediaProperties[resourceService.GetString(ResourceName.PropertyComposers)] = string.Join("; ", media.MediaInfo.MusicProperties.Composers);
                    MediaProperties[resourceService.GetString(ResourceName.PropertyGenre)] = string.Join("; ", media.MediaInfo.MusicProperties.Genre);
                    MediaProperties[resourceService.GetString(ResourceName.PropertyTrack)] = media.MediaInfo.MusicProperties.TrackNumber.ToString();
                    MediaProperties[resourceService.GetString(ResourceName.PropertyYear)] = media.MediaInfo.MusicProperties.Year > 0
                        ? media.MediaInfo.MusicProperties.Year.ToString()
                        : string.Empty;
                    MediaProperties[resourceService.GetString(ResourceName.PropertyLength)] = Humanizer.ToDuration((TimeSpan)media.MediaInfo.MusicProperties.Duration);

                    AudioProperties[resourceService.GetString(ResourceName.PropertyBitRate)] = $"{media.MediaInfo.MusicProperties.Bitrate / 1000} kbps";
                    break;
            }


            switch (media.Source)
            {
                case StorageFile file:
                    mediaFile = file;
                    CanNavigateToFile = true;
                    FileProperties[resourceService.GetString(ResourceName.PropertyFileType)] = mediaFile.FileType;
                    FileProperties[resourceService.GetString(ResourceName.PropertyContentType)] = mediaFile.ContentType;
                    FileProperties[resourceService.GetString(ResourceName.PropertySize)] = BytesToHumanReadable((long)media.MediaInfo.Size);
                    FileProperties[resourceService.GetString(ResourceName.PropertyLastModified)] = media.MediaInfo.DateModified.ToString();
                    break;
                case Uri uri:
                    mediaUri = uri;
                    CanNavigateToFile = uri.IsFile;
                    break;
            }
        }

        [RelayCommand(CanExecute = nameof(Screenbox.Core.ViewModels.PropertyViewModel.CanNavigateToFile))]
        private void OpenFileLocation()
        {
            if (mediaFile != null)
                filesService.OpenFileLocationAsync(mediaFile);
            else if (mediaUri != null)
                filesService.OpenFileLocationAsync(mediaUri.OriginalString);
        }

        // https://stackoverflow.com/a/11124118
        private static string BytesToHumanReadable(long byteCount)
        {
            // Get absolute value
            long absCount = (byteCount < 0 ? -byteCount : byteCount);
            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absCount >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = (byteCount >> 50);
            }
            else if (absCount >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (byteCount >> 40);
            }
            else if (absCount >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (byteCount >> 30);
            }
            else if (absCount >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (byteCount >> 20);
            }
            else if (absCount >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (byteCount >> 10);
            }
            else if (absCount >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = byteCount;
            }
            else
            {
                return byteCount.ToString("0 B"); // Byte
            }
            // Divide by 1024 to get fractional value
            readable = (readable / 1024);
            // Return formatted number with suffix
            return readable.ToString("0.## ") + suffix;
        }
    }
}
