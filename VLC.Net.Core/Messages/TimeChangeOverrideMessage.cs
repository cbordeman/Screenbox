using CommunityToolkit.Mvvm.Messaging.Messages;

namespace VLC.Net.Core.Messages
{
    public sealed class TimeChangeOverrideMessage : ValueChangedMessage<bool>
    {
        public TimeChangeOverrideMessage(bool value) : base(value)
        {
        }
    }
}
