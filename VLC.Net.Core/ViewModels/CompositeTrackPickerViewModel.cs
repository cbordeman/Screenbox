#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using VLC.Net.Core.Enums;
using VLC.Net.Core.Messages;
using VLC.Net.Core.Playback;
using VLC.Net.Core.Services;
using AudioTrack = VLC.Net.Core.Playback.AudioTrack;
using SubtitleTrack = VLC.Net.Core.Playback.SubtitleTrack;
using VideoTrack = VLC.Net.Core.Playback.VideoTrack;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class CompositeTrackPickerViewModel : ObservableRecipient,
        IRecipient<PlaylistCurrentItemChangedMessage>,
        IRecipient<MediaPlayerChangedMessage>
    {
        public ObservableCollection<string> SubtitleTracks { get; }

        public ObservableCollection<string> AudioTracks { get; }

        public ObservableCollection<string> VideoTracks { get; }

        private PlaybackSubtitleTrackList? ItemSubtitleTrackList => mediaPlayer?.PlaybackItem?.SubtitleTracks;

        private PlaybackAudioTrackList? ItemAudioTrackList => mediaPlayer?.PlaybackItem?.AudioTracks;

        private PlaybackVideoTrackList? ItemVideoTrackList => mediaPlayer?.PlaybackItem?.VideoTracks;

        [ObservableProperty] private int subtitleTrackIndex;
        [ObservableProperty] private int audioTrackIndex;
        [ObservableProperty] private int videoTrackIndex;
        private readonly IFilesService filesService;
        private readonly IResourceService resourceService;
        private readonly ISettingsService settingsService;
        private IMediaPlayer? mediaPlayer;
        private bool flyoutOpened;
        private CancellationTokenSource? cts;

        public CompositeTrackPickerViewModel(IFilesService filesService, IResourceService resourceService, ISettingsService settingsService)
        {
            this.filesService = filesService;
            this.resourceService = resourceService;
            this.settingsService = settingsService;
            SubtitleTracks = new ObservableCollection<string>();
            AudioTracks = new ObservableCollection<string>();
            VideoTracks = new ObservableCollection<string>();
            mediaPlayer = Messenger.Send(new MediaPlayerRequestMessage()).Response;

            IsActive = true;
        }

        public void Receive(MediaPlayerChangedMessage message)
        {
            mediaPlayer = message.Value;
        }

        /// <summary>
        /// Try load a subtitle in the same directory with the same name
        /// </summary>
        public async void Receive(PlaylistCurrentItemChangedMessage message)
        {
            cts?.Cancel();
            if (mediaPlayer is not VlcMediaPlayer player) return;
            if (message.Value is not { Source: StorageFile file, MediaType: MediaPlaybackType.Video } media)
                return;

            bool subtitleInitialized = false;
            var playbackSubtitleTrackList = media.Item.Value?.SubtitleTracks;
            if (playbackSubtitleTrackList == null) return;
            if (playbackSubtitleTrackList.Count > 0) subtitleInitialized = true;
            IReadOnlyList<StorageFile> subtitles = await GetSubtitlesForFile(file);
            foreach (StorageFile subtitleFile in subtitles)
            {
                // Preload subtitle but don't select it
                playbackSubtitleTrackList.AddExternalSubtitle(player, subtitleFile, false);
            }

            if (!subtitleInitialized && media.Item.Value is { } playbackItem)
            {
                try
                {
                    using var cts = new CancellationTokenSource();
                    this.cts = cts;
                    await playbackItem.Media.WaitForParsed(TimeSpan.FromSeconds(5), cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // pass
                }
                finally
                {
                    cts = null;
                }
            }

            TrySetSubtitleFromLanguage(playbackSubtitleTrackList, settingsService.PersistentSubtitleLanguage);
        }

        private static void TrySetSubtitleFromLanguage(PlaybackSubtitleTrackList subtitleTrackList, string persistentLanguage)
        {
            // Check persistent subtitle value to try and select a subtitle
            if (!string.IsNullOrEmpty(persistentLanguage))
            {
                // If there is only one subtitle then select it
                if (subtitleTrackList.Count == 1)
                {
                    subtitleTrackList.SelectedIndex = 0;
                    return;
                }

                // Try to select the subtitle with the same language as the persistent value
                var langPreferences = persistentLanguage.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (string language in langPreferences)
                {
                    for (int i = 0; i < subtitleTrackList.Count; i++)
                    {
                        var subtitleTrack = subtitleTrackList[i];
                        // Try to match language tag first, then language name
                        if (language == subtitleTrack.LanguageTag || language.Equals(subtitleTrack.Language, StringComparison.CurrentCultureIgnoreCase))
                        {
                            subtitleTrackList.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        private async Task<IReadOnlyList<StorageFile>> GetSubtitlesForFile(StorageFile sourceFile)
        {
            IReadOnlyList<StorageFile> subtitles = Array.Empty<StorageFile>();
            StorageFileQueryResult? query = Messenger.Send<PlaylistRequestMessage>().Response.NeighboringFilesQuery;
            if (query != null)
            {
                try
                {
                    IReadOnlyList<StorageFile> files = await query.GetFilesAsync(0, 50);
                    subtitles = files.Where(f =>
                            f.IsSupportedSubtitle() && f.Name.StartsWith(
                                Path.GetFileNameWithoutExtension(sourceFile.Name),
                                StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                catch (Exception e)
                {
                    LogService.Log(e);
                }
            }
            else
            {
                QueryOptions options = new(CommonFileQuery.DefaultQuery, FilesHelpers.SupportedSubtitleFormats)
                {
                    ApplicationSearchFilter = $"System.FileName:$<\"{Path.GetFileNameWithoutExtension(sourceFile.Name)}\""
                };

                query = await filesService.GetNeighboringFilesQueryAsync(sourceFile, options);
                if (query != null)
                {
                    subtitles = await query.GetFilesAsync(0, 50);
                }
            }

            return subtitles;
        }

        partial void OnSubtitleTrackIndexChanged(int value)
        {
            if (ItemSubtitleTrackList != null && value >= 0 && value < SubtitleTracks.Count)
            {
                ItemSubtitleTrackList.SelectedIndex = value - 1;

                if (flyoutOpened)
                {
                    if (value == 0)
                    {
                        settingsService.PersistentSubtitleLanguage = string.Empty;
                    }
                    else
                    {
                        var subtitle = ItemSubtitleTrackList[ItemSubtitleTrackList.SelectedIndex];
                        settingsService.PersistentSubtitleLanguage =
                            $"{subtitle.LanguageTag},{subtitle.Language},{LanguageHelper.GetPreferredLanguage().Substring(0, 2)}";
                    }
                }
            }

        }

        partial void OnAudioTrackIndexChanged(int value)
        {
            if (ItemAudioTrackList != null && value >= 0 && value < AudioTracks.Count)
                ItemAudioTrackList.SelectedIndex = value;
        }

        partial void OnVideoTrackIndexChanged(int value)
        {
            if (ItemVideoTrackList != null && value >= 0 && value < VideoTracks.Count)
                ItemVideoTrackList.SelectedIndex = value;
        }

        [RelayCommand]
        private async Task AddSubtitle()
        {
            if (ItemSubtitleTrackList == null || mediaPlayer is not VlcMediaPlayer player) return;
            try
            {
                StorageFile? file = await filesService.PickFileAsync(FilesHelpers.SupportedSubtitleFormats.Add("*").ToArray());
                if (file == null) return;

                ItemSubtitleTrackList.AddExternalSubtitle(player, file, true);
                Messenger.Send(new SubtitleAddedNotificationMessage(file));
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(
                    resourceService.GetString(ResourceName.FailedToLoadSubtitleNotificationTitle), e.ToString()));
            }
        }

        public void OnFlyoutOpening()
        {
            UpdateSubtitleTrackList();
            UpdateAudioTrackList();
            UpdateVideoTrackList();
            SubtitleTrackIndex = ItemSubtitleTrackList?.SelectedIndex + 1 ?? 0;
            AudioTrackIndex = ItemAudioTrackList?.SelectedIndex ?? -1;
            VideoTrackIndex = ItemVideoTrackList?.SelectedIndex ?? -1;

            flyoutOpened = true;
        }

        public void OnFlyoutClosed()
        {
            flyoutOpened = false;
        }

        private void UpdateAudioTrackList()
        {
            if (ItemAudioTrackList == null) return;
            AudioTracks.Clear();
            ItemAudioTrackList.Refresh();
            if (ItemAudioTrackList.Count <= 0) return;

            for (int index = 0; index < ItemAudioTrackList.Count; index++)
            {
                AudioTrack audioTrack = ItemAudioTrackList[index];
                string defaultTrackLabel = resourceService.GetString(ResourceName.TrackIndex, index + 1);
                AudioTracks.Add(string.IsNullOrEmpty(audioTrack.Label) ? defaultTrackLabel : audioTrack.Label);
            }
        }

        private void UpdateVideoTrackList()
        {
            if (ItemVideoTrackList == null) return;
            VideoTracks.Clear();
            ItemVideoTrackList.Refresh();
            if (ItemVideoTrackList.Count <= 0) return;

            for (int index = 0; index < ItemVideoTrackList.Count; index++)
            {
                VideoTrack videoTrack = ItemVideoTrackList[index];
                string defaultTrackLabel = resourceService.GetString(ResourceName.TrackIndex, index + 1);
                VideoTracks.Add(string.IsNullOrEmpty(videoTrack.Label) ? defaultTrackLabel : videoTrack.Label);
            }
        }

        private void UpdateSubtitleTrackList()
        {
            if (ItemSubtitleTrackList == null) return;
            SubtitleTracks.Clear();
            SubtitleTracks.Add(resourceService.GetString(ResourceName.Disable));
            if (ItemSubtitleTrackList.Count <= 0) return;

            for (int index = 0; index < ItemSubtitleTrackList.Count; index++)
            {
                SubtitleTrack subtitleTrack = ItemSubtitleTrackList[index];
                string defaultTrackLabel = resourceService.GetString(ResourceName.TrackIndex, index + 1);
                SubtitleTracks.Add(string.IsNullOrEmpty(subtitleTrack.Label) ? defaultTrackLabel : subtitleTrack.Label);
            }
        }
    }
}
