#nullable enable

using CommunityToolkit.Mvvm.Messaging.Messages;
using MediaViewModel = VLC.Net.Core.ViewModels.MediaViewModel;

namespace VLC.Net.Core.Messages
{
    public sealed class PlaylistCurrentItemChangedMessage : ValueChangedMessage<ViewModels.MediaViewModel?>
    {
        public PlaylistCurrentItemChangedMessage(MediaViewModel? value) : base(value)
        {
        }
    }
}
