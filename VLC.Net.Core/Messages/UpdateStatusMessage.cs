#nullable enable

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace VLC.Net.Core.Messages
{
    public sealed class UpdateStatusMessage : ValueChangedMessage<string?>
    {
        public UpdateStatusMessage(string? value) : base(value)
        {
        }
    }
}
