#nullable enable

using Avalonia.Platform.Storage;
using VLC.Net.Core.Models;

namespace VLC.Net.Core.Services;
public interface ILivelyWallpaperService
{
    Task<List<LivelyWallpaperModel>> GetAvailableVisualizersAsync();
    Task<LivelyWallpaperModel?> InstallVisualizerAsync(IStorageFile wallpaperFile);
}
