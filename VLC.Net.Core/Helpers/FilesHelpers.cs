using System.Collections.Immutable;
using Avalonia.Platform.Storage;
using VLC.Net.Core.Enums;

namespace VLC.Net.Core.Helpers;

public static class FilesHelpers
{
    public static ImmutableArray<string> SupportedAudioFormats { get; } =
        [".mp3", ".wav", ".wma", ".aac", ".mid", ".midi", ".mpa", 
            ".ogg", ".oga", ".opus", ".weba", ".flac", ".m4a", 
            ".m4b", ".wv", ".wvc", ".aiff", ".aif", ".aifc", ".ac3", 
            ".ape", ".dts", ".nist", ".ra", ".spx"];

    public static ImmutableArray<string> SupportedVideoFormats { get; } =
        ImmutableArray.Create(".avi", ".mp4", ".wmv", ".mov", ".mkv", 
            ".flv", ".3gp", ".3g2", ".m4v", ".mpg", ".mpeg", ".webm", 
            ".rm", ".rmvb", ".asf", ".wm", ".wtv", ".f4v", ".swf", 
            ".vob", ".mxf");

    public static ImmutableArray<string> SupportedPlaylistFormats { get; } =
        ImmutableArray.Create(".m3u8", ".m3u", ".ts", ".mts", ".m2ts", ".m2t");

    public static ImmutableArray<string> SupportedFormats { get; } =
        SupportedVideoFormats.AddRange(SupportedAudioFormats).AddRange(SupportedPlaylistFormats);

    public static ImmutableArray<string> SupportedSubtitleFormats { get; } = ImmutableArray.Create(".srt", ".vtt", ".ass", ".idx", ".sub");

    public static bool IsSupportedAudio(this IStorageFile file) => 
        SupportedAudioFormats.Contains(file.GetFileType());
    public static bool IsSupportedVideo(this IStorageFile file) => 
        SupportedVideoFormats.Contains(file.GetFileType());
    public static bool IsSupportedPlaylist(this IStorageFile file) => 
        SupportedPlaylistFormats.Contains(file.GetFileType());
    public static bool IsSupported(this IStorageFile file) => 
        SupportedFormats.Contains(file.GetFileType());
    public static bool IsSupportedSubtitle(this IStorageFile file) => 
        SupportedSubtitleFormats.Contains(file.GetFileType());

    /// <summary>
    /// Gets the file extension in lower case, including the dot, invariant.
    /// </summary>
    /// <returns></returns>
    public static string GetFileType(this IStorageFile file)
    {
        var ext = Path.GetExtension(file.Path.GetFilePath()).ToLowerInvariant();
        return ext;
    }
    
    public static MediaPlaybackType GetMediaTypeForFile(IStorageFile file)
    {
        if (file.IsSupportedVideo()) return MediaPlaybackType.Video;
        if (file.IsSupportedAudio()) return MediaPlaybackType.Music;
        if (file.GetContentType().StartsWith("image")) return MediaPlaybackType.Image;
        if (file.IsSupportedPlaylist()) return MediaPlaybackType.Playlist;
        return MediaPlaybackType.Unknown;
    }

    public static string GetContentType(this IStorageFile file)
    {
        if (MimeTypes.TryGetMimeType(file.Path.GetFilePath().ToString(), out string? type))
            return type;
        return string.Empty;
    }

    public static string GetFilePath(this Uri uri)
    {
        // Use Host + AbsolutePath to get part without scheme and query
        string result = uri.Host + uri.AbsolutePath;
        return result;
    }

    public static string GetDisplayName(this IStorageFile file)
    {
        return Path.GetFileNameWithoutExtension(file.Path.GetFilePath());
    }

    public static DateTimeOffset GetDateCreated(this IStorageItem item)
    {
        var properties = item.GetBasicPropertiesAsync().Result;
        var dateCreated = properties.DateCreated;
    }
}