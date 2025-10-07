using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;

namespace VLC.Net.Core.Helpers;

public static class TopLevelHelper
{
    public static TopLevel GetTopLevel()
    {
        var desktopLifetime = Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var mainWindow = desktopLifetime!.MainWindow;
    }
}
