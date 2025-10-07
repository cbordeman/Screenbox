﻿using VLC.Net.Core.Enums;

namespace VLC.Net.Core.Helpers;
public static class EnumExtensions
{
    public static ElementTheme ToElementTheme(this ThemeOption themeOption)
    {
        return themeOption switch
        {
            ThemeOption.Auto => ElementTheme.Default,
            ThemeOption.Light => ElementTheme.Light,
            ThemeOption.Dark => ElementTheme.Dark,
            _ => throw new ArgumentOutOfRangeException(nameof(themeOption), themeOption, null),
        };
    }
}
