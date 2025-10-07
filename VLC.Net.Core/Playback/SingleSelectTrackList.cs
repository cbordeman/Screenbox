#nullable enable

using System.Collections;

namespace VLC.Net.Core.Playback
{
    public class SingleSelectTrackList<T> : IReadOnlyList<T>, ISingleSelectMediaTrackList where T : IMediaTrack
    {
        public event TypedEventHandler<ISingleSelectMediaTrackList, object?>? SelectedIndexChanged;

        public T this[int index] => TrackList[index];

        public int Count => TrackList.Count;

        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                if (value == selectedIndex) return;
                selectedIndex = value;
                SelectedIndexChanged?.Invoke(this, null);
            }
        }

        protected List<T> TrackList { get; }

        private int selectedIndex;

        public SingleSelectTrackList()
        {
            TrackList = new List<T>();
            selectedIndex = -1;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return TrackList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return TrackList.GetEnumerator();
        }
    }
}