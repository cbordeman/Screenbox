#nullable enable

using LibVLCSharp.Shared;

namespace VLC.Net.Core.Playback
{
    public sealed class PlaybackVideoTrackList : SingleSelectTrackList<VideoTrack>
    {
        private readonly Media? media;
        private readonly MediaPlaybackVideoTrackList? source;

        public PlaybackVideoTrackList(Media media)
        {
            this.media = media;
            if (this.media.Tracks.Length > 0)
            {
                AddVlcMediaTracks(this.media.Tracks);
            }
            else
            {
                this.media.ParsedChanged += Media_ParsedChanged;
            }

            SelectedIndex = 0;
        }

        public PlaybackVideoTrackList(MediaPlaybackVideoTrackList source)
        {
            this.source = source;
            SelectedIndex = source.SelectedIndex;
            source.SelectedIndexChanged += (sender, args) => SelectedIndex = sender.SelectedIndex;
            foreach (Windows.Media.Core.VideoTrack videoTrack in source)
            {
                TrackList.Add(new VideoTrack(videoTrack));
            }

            SelectedIndexChanged += OnSelectedIndexChanged;
        }

        public void Refresh()
        {
            if (source == null) return;
            TrackList.Clear();
            foreach (Windows.Media.Core.VideoTrack videoTrack in source)
            {
                TrackList.Add(new VideoTrack(videoTrack));
            }
        }

        private void OnSelectedIndexChanged(ISingleSelectMediaTrackList sender, object? args)
        {
            // Only update for Windows track list. VLC track list is handled by the player.
            if (source == null || source.SelectedIndex == sender.SelectedIndex) return;
            source.SelectedIndex = sender.SelectedIndex;
        }

        private void Media_ParsedChanged(object sender, MediaParsedChangedEventArgs e)
        {
            if (media == null || e.ParsedStatus != MediaParsedStatus.Done) return;
            media.ParsedChanged -= Media_ParsedChanged;
            AddVlcMediaTracks(media.Tracks);
        }

        private void AddVlcMediaTracks(LibVLCSharp.Shared.MediaTrack[] tracks)
        {
            foreach (LibVLCSharp.Shared.MediaTrack track in tracks)
            {
                if (track.TrackType == TrackType.Video)
                {
                    TrackList.Add(new VideoTrack(track));
                }
            }
        }
    }
}
