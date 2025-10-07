using CommunityToolkit.Mvvm.Messaging.Messages;

namespace VLC.Net.Core.Messages
{
    public sealed class ChangeAspectRatioMessage : ValueChangedMessage<Size>
    {
        public ChangeAspectRatioMessage(Size value) : base(value)
        {
        }
    }
}
