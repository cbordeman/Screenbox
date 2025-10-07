using CommunityToolkit.Mvvm.Messaging.Messages;

namespace VLC.Net.Core.Messages
{
    public sealed class RaiseFrameSavedNotificationMessage : ValueChangedMessage<StorageFile>
    {
        public RaiseFrameSavedNotificationMessage(StorageFile value) : base(value)
        {
        }
    }
}
