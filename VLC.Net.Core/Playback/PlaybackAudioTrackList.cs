#nullable enable

using LibVLCSharp.Shared;

namespace VLC.Net.Core.Playback
{
    public sealed class PlaybackAudioTrackList : SingleSelectTrackList<AudioTrack>
    {
        private readonly Media? media;
        private readonly MediaPlaybackAudioTrackList? source;

        public PlaybackAudioTrackList(Media media)
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

        public PlaybackAudioTrackList(MediaPlaybackAudioTrackList source)
        {
            this.source = source;
            SelectedIndex = source.SelectedIndex;
            source.SelectedIndexChanged += (sender, args) => SelectedIndex = sender.SelectedIndex;
            foreach (Windows.Media.Core.AudioTrack audioTrack in source)
            {
                TrackList.Add(new AudioTrack(audioTrack));
            }

            SelectedIndexChanged += OnSelectedIndexChanged;
        }

        public void Refresh()
        {
            if (source == null) return;
            TrackList.Clear();
            foreach (Windows.Media.Core.AudioTrack audioTrack in source)
            {
                TrackList.Add(new AudioTrack(audioTrack));
            }
        }

        private void OnSelectedIndexChanged(ISingleSelectMediaTrackList sender, object? args)
        {
            // Only update for Windows track list. VLC track list is handled by the player.
            if (source == null || source.SelectedIndex == sender.SelectedIndex) return;
            source.SelectedIndex = sender.SelectedIndex;
        }

        //public PlaybackAudioTrackList(MediaPlaybackItem playbackItem)
        //{
        //    playbackItem.AudioTracksChanged += PlaybackItem_AudioTracksChanged;
        //    MediaPlaybackAudioTrackList audioTracks = playbackItem.AudioTracks;
        //    foreach (Windows.Media.Core.AudioTrack track in audioTracks)
        //    {
        //        TrackList.Add(new AudioTrack(track));
        //    }

        //    SelectedIndex = audioTracks.SelectedIndex;
        //    audioTracks.SelectedIndexChanged += AudioTracks_SelectedIndexChanged;
        //}

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
                if (track.TrackType == TrackType.Audio)
                {
                    TrackList.Add(new AudioTrack(track));
                }
            }
        }

        //private void PlaybackItem_AudioTracksChanged(MediaPlaybackItem sender, IVectorChangedEventArgs args)
        //{
        //    int index = (int)args.Index;
        //    switch (args.CollectionChange)
        //    {
        //        case CollectionChange.Reset:
        //            TrackList.Clear();
        //            foreach (Windows.Media.Core.AudioTrack track in sender.AudioTracks)
        //            {
        //                TrackList.Add(new AudioTrack(track));
        //            }

        //            break;
        //        case CollectionChange.ItemInserted:
        //            TrackList.Insert(index, new AudioTrack(sender.AudioTracks[index]));
        //            break;
        //        case CollectionChange.ItemRemoved:
        //            TrackList.RemoveAt(index);
        //            break;
        //        case CollectionChange.ItemChanged:
        //        default:
        //            // Not implemented
        //            break;
        //    }
        //}

        //private void AudioTracks_SelectedIndexChanged(ISingleSelectMediaTrackList sender, object args)
        //{
        //    SelectedIndex = sender.SelectedIndex;
        //}
    }
}
