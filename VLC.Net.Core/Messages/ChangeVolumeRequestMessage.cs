﻿using CommunityToolkit.Mvvm.Messaging.Messages;

namespace VLC.Net.Core.Messages
{
    public sealed class ChangeVolumeRequestMessage : RequestMessage<int>
    {
        public bool IsOffset { get; }

        public int Value { get; }

        public ChangeVolumeRequestMessage(int value, bool offset = false)
        {
            Value = value;
            IsOffset = offset;
        }
    }
}
