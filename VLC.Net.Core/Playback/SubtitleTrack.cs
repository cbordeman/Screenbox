#nullable enable

using LibVLCSharp.Shared;

namespace VLC.Net.Core.Playback
{
    public sealed class SubtitleTrack : MediaTrack
    {
        internal int VlcSpu { get; set; }

        public SubtitleTrack(string language = "") : base(MediaTrackKind.TimedMetadata, language)
        {
        }

        public SubtitleTrack(LibVLCSharp.Shared.MediaTrack textTrack) : base(textTrack)
        {
            Guard.IsTrue(textTrack.TrackType == TrackType.Text, nameof(textTrack.TrackType));
            VlcSpu = textTrack.Id;
        }

        public SubtitleTrack(TimedMetadataTrack track) : base(track)
        {
        }
    }
}
