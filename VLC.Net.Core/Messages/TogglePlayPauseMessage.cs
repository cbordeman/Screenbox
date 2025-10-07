﻿namespace VLC.Net.Core.Messages;

public class TogglePlayPauseMessage
{
    public bool ShowBadge { get; }

    public TogglePlayPauseMessage(bool showBadge)
    {
        ShowBadge = showBadge;
    }
}