#nullable enable

using CommunityToolkit.Mvvm.Messaging.Messages;
using VLC.Net.Core.Playback;

namespace VLC.Net.Core.Messages
{
    public sealed class MediaPlayerRequestMessage : RequestMessage<IMediaPlayer?>
    {
    }
}
