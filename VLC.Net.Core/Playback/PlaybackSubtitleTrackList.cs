using LibVLCSharp.Shared;

namespace VLC.Net.Core.Playback
{
    public sealed class PlaybackSubtitleTrackList : SingleSelectTrackList<SubtitleTrack>
    {
        private readonly Media media;
        private readonly List<LazySubtitleTrack> pendingSubtitleTracks;

        private class LazySubtitleTrack
        {
            public SubtitleTrack Track { get; }

            public StorageFile File { get; }

            public VlcMediaPlayer Player { get; }

            public LazySubtitleTrack(VlcMediaPlayer player, StorageFile file)
            {
                Player = player;
                File = file;
                Track = new SubtitleTrack
                {
                    Id = "-1",
                    VlcSpu = -1,
                    Label = file.Name,
                };
            }
        }

        private int delaySpu = -1;

        public PlaybackSubtitleTrackList(Media media)
        {
            pendingSubtitleTracks = new List<LazySubtitleTrack>();
            this.media = media;
            if (this.media.Tracks.Length > 0)
            {
                AddVlcMediaTracks(this.media.Tracks);
            }
            else
            {
                this.media.ParsedChanged += Media_ParsedChanged;
            }

            SelectedIndexChanged += OnSelectedIndexChanged;
        }

        internal void SelectVlcSpu(int spu)
        {
            if (spu < 0)
            {
                SelectedIndex = -1;
                return;
            }

            // Spu may be set before tracks are populated. Delay select.
            if (Count == 0)
            {
                delaySpu = spu;
                return;
            }

            for (int i = 0; i < Count; i++)
            {
                if (this[i].VlcSpu == spu)
                {
                    SelectedIndex = i;
                    break;
                }
            }
        }

        private void OnSelectedIndexChanged(ISingleSelectMediaTrackList sender, object args)
        {
            if (SelectedIndex >= 0 && TrackList[SelectedIndex] is { } selectedTrack &&
                pendingSubtitleTracks.FirstOrDefault(x => ReferenceEquals(x.Track, selectedTrack)) is { } lazyTrack &&
                (selectedTrack.VlcSpu == -1 || lazyTrack.Player.VlcPlayer.SpuCount < selectedTrack.VlcSpu))
            {
                selectedTrack.VlcSpu = -1;
                lazyTrack.Player.AddSubtitle(lazyTrack.File, true);
            }
        }

        public void AddExternalSubtitle(VlcMediaPlayer player, StorageFile file, bool select)
        {
            var lazySub = new LazySubtitleTrack(player, file);
            pendingSubtitleTracks.Add(lazySub);
            TrackList.Add(lazySub.Track);

            if (select)
            {
                SelectedIndex = TrackList.Count - 1;
            }
        }

        internal void NotifyTrackAdded(int trackId)
        {
            if (SelectedIndex >= 0 && TrackList[SelectedIndex] is { VlcSpu: -1 } selectedTrack)
            {
                selectedTrack.VlcSpu = trackId;
                selectedTrack.Id = trackId.ToString();
            }
        }

        private void Media_ParsedChanged(object sender, MediaParsedChangedEventArgs e)
        {
            if (e.ParsedStatus != MediaParsedStatus.Done) return;
            media.ParsedChanged -= Media_ParsedChanged;
            AddVlcMediaTracks(media.Tracks);
            if (delaySpu >= 0)
                SelectVlcSpu(delaySpu);
        }

        private void AddVlcMediaTracks(LibVLCSharp.Shared.MediaTrack[] tracks)
        {
            foreach (LibVLCSharp.Shared.MediaTrack track in tracks)
            {
                if (track.TrackType == TrackType.Text)
                {
                    TrackList.Add(new SubtitleTrack(track));
                }
            }
        }
    }
}
