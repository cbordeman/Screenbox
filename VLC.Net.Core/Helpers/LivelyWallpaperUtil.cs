using System.IO.Compression;
using VLC.Net.Core.Enums;
using VLC.Net.Core.Models;

namespace VLC.Net.Core.Helpers;

// Copyright (c) Dani John
// Licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/rocksdanister/lively
public static class LivelyWallpaperUtil
{
    public static async Task<bool> IsWallpaperFile(ZipArchive wallpaperFile)
    {
        try
        {
            var livelyInfoEntry = wallpaperFile.GetEntry("LivelyInfo.json");
            if (livelyInfoEntry != null)
                return true;
        }
        catch { /* Ignore */ }
        return false;
    }

    public static bool IsLocalWebWallpaper(this LivelyInfoModel model)
    {
        return model.Type == LivelyWallpaperType.Web || model.Type == LivelyWallpaperType.Webaudio;
    }

    public static bool IsAudioWallpaper(this LivelyInfoModel model)
    {
        // Backward compatibility with old wallpaper file.
        if (model.Type == LivelyWallpaperType.Webaudio)
            return true;

        return IsWallpaperArgPresent(model, "--audio");
    }

    public static bool IsMusicWallpaper(this LivelyInfoModel model)
    {
        return IsWallpaperArgPresent(model, "--system-nowplaying");
    }

    public static bool IsPauseNotify(this LivelyInfoModel model)
    {
        return IsWallpaperArgPresent(model, "--pause-event true");
    }

    private static bool IsWallpaperArgPresent(LivelyInfoModel model, string arg)
    {
        return !string.IsNullOrWhiteSpace(model.Arguments) && model.Arguments.Contains(arg);
    }
}
