using CommunityToolkit.Mvvm.Messaging.Messages;
using VLC.Net.Core.Models;

namespace VLC.Net.Core.Messages
{
    public sealed class PlaylistRequestMessage : RequestMessage<PlaylistInfo>
    {
    }
}
