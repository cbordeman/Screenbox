using CommunityToolkit.Mvvm.Messaging.Messages;

namespace VLC.Net.Core.Messages
{
    public sealed class UpdateVolumeStatusMessage : ValueChangedMessage<int>
    {
        public UpdateVolumeStatusMessage(int value) : base(value)
        {
        }
    }
}
