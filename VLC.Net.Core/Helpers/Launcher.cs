using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System.IO;
using System.Threading.Tasks;

namespace VLC.Net.Core.Helpers;

public static class Launcher
{
    /// <summary>
    /// Opens Explorer on Windows, other 
    /// </summary>
    /// <param name="fileOrFolderPath"></param>
    /// <exception cref="PlatformNotSupportedException"></exception>
    public static void OpenFileOrFolderInUi(string fileOrFolderPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo("explorer.exe", fileOrFolderPath)
            {
                UseShellExecute = true
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // OSX will open the file, so ensure a folder is passed.
            var folder = File.Exists(fileOrFolderPath) ?
                Path.GetDirectoryName(fileOrFolderPath) : fileOrFolderPath;
            if (folder is not null && Directory.Exists(folder))
                Process.Start("open", folder);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux will open the file, so ensure a folder is passed.
            var folder = File.Exists(fileOrFolderPath) ?
                Path.GetDirectoryName(fileOrFolderPath) : fileOrFolderPath;
            if (folder is not null && Directory.Exists(folder))
                Process.Start("xdg-open", folder);
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported platform");
        }
    }
}