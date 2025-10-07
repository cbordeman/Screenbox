using CommunityToolkit.Mvvm.Messaging.Messages;
using MediaViewModel = VLC.Net.Core.ViewModels.MediaViewModel;

namespace VLC.Net.Core.Messages
{
    public sealed class QueuePlaylistMessage : ValueChangedMessage<IEnumerable<ViewModels.MediaViewModel>>
    {
        public bool AddNext { get; }

        public QueuePlaylistMessage(MediaViewModel target, bool addNext = false) : base(new[] { target })
        {
            AddNext = addNext;
        }

        public QueuePlaylistMessage(IEnumerable<MediaViewModel> playlist, bool addNext = false) : base(playlist)
        {
            AddNext = addNext;
        }
    }
}
