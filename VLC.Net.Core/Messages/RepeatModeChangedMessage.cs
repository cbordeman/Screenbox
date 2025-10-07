using CommunityToolkit.Mvvm.Messaging.Messages;

namespace VLC.Net.Core.Messages
{
    public sealed class RepeatModeChangedMessage : ValueChangedMessage<MediaPlaybackAutoRepeatMode>
    {
        public RepeatModeChangedMessage(MediaPlaybackAutoRepeatMode value) : base(value)
        {
        }
    }
}
