using CommunityToolkit.Mvvm.Messaging.Messages;

namespace VLC.Net.Core.Messages
{
    public class RaiseResumePositionNotificationMessage : ValueChangedMessage<TimeSpan>
    {
        public RaiseResumePositionNotificationMessage(TimeSpan value) : base(value)
        {
        }
    }
}
