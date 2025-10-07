using CommunityToolkit.Mvvm.Messaging.Messages;

namespace VLC.Net.Core.Messages;

public class PlayerControlsVisibilityChangedMessage : ValueChangedMessage<bool>
{
    public PlayerControlsVisibilityChangedMessage(bool value) : base(value)
    {
    }
}
