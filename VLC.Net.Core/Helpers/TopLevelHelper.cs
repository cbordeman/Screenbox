using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace VLC.Net.Core.Helpers;

/// <summary>
/// These helpers only work on Desktop, not mobile.  They are intended
/// to help in the implementation of desktop services.
/// </summary>
public static class TopLevelDesktopHelper
{
    /// <summary>
    /// Only works on desktop.
    /// </summary>
    public static TopLevel GetTopLevel()
    {
        var desktopLifetime = Application.Current!.ApplicationLifetime 
            as IClassicDesktopStyleApplicationLifetime;
        var mainWindow = desktopLifetime?.MainWindow;
        if (mainWindow == null)
            throw new InvalidOperationException("Must have a main window.");
        var tl = TopLevel.GetTopLevel(mainWindow);
        if (tl == null)
            throw new InvalidOperationException("Could not get top level.");
        return tl;
    }

    /// <summary>
    /// Only works on Desktop. 
    /// </summary>
    public static Task<IStorageFile?> GetIStorageFileFromPath(string? filePath)
    {
        if (filePath == null)
            return Task.FromResult<IStorageFile?>(null);
        var storageProvider = GetTopLevel().StorageProvider;
        return storageProvider.TryGetFileFromPathAsync(filePath);
    }
}