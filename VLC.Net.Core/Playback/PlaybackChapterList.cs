using System.Collections.ObjectModel;
using LibVLCSharp.Shared.Structures;

namespace VLC.Net.Core.Playback
{
    public sealed class PlaybackChapterList : ReadOnlyCollection<ChapterCue>
    {
        private readonly List<ChapterCue> chapters;
        private readonly PlaybackItem item;

        internal PlaybackChapterList(PlaybackItem item) : base(new List<ChapterCue>())
        {
            this.item = item;
            chapters = (List<ChapterCue>)Items;
        }

        public void Load(IMediaPlayer player)
        {
            if (player is not VlcMediaPlayer vlcPlayer || player.PlaybackItem != item)
                return;

            if (vlcPlayer.VlcPlayer.ChapterCount > 0)
            {
                List<ChapterDescription> chapterDescriptions = new();
                for (int i = 0; i < vlcPlayer.VlcPlayer.TitleCount; i++)
                {
                    chapterDescriptions.AddRange(vlcPlayer.VlcPlayer.FullChapterDescriptions(i));
                }

                Load(chapterDescriptions);
            }
            else
            {
                Load(vlcPlayer.VlcPlayer.FullChapterDescriptions());
            }

            vlcPlayer.Chapter = chapters.FirstOrDefault();
        }

        private void Load(IEnumerable<ChapterDescription> vlcChapters)
        {
            IEnumerable<ChapterCue> chapterCues = vlcChapters.Select(c => new ChapterCue
            {
                Title = c.Name ?? string.Empty,
                Duration = TimeSpan.FromMilliseconds(c.Duration),
                StartTime = TimeSpan.FromMilliseconds(c.TimeOffset)
            });

            chapters.Clear();
            chapters.AddRange(chapterCues);
        }
    }
}
