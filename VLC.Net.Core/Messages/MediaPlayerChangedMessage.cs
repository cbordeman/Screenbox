using CommunityToolkit.Mvvm.Messaging.Messages;
using VLC.Net.Core.Playback;

namespace VLC.Net.Core.Messages
{
    public sealed class MediaPlayerChangedMessage : ValueChangedMessage<IMediaPlayer>
    {
        public MediaPlayerChangedMessage(IMediaPlayer value) : base(value)
        {
        }
    }
}
