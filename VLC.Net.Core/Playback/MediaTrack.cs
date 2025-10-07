#nullable enable

using LibVLCSharp.Shared;
using VLC.Net.Core.Helpers;

namespace VLC.Net.Core.Playback;
public abstract class MediaTrack : IMediaTrack
{
    public string Id { get; internal set; }

    public string Label { get; set; }

    public string Language => language?.DisplayName ?? languageStr;

    public string LanguageTag => language?.LanguageTag ?? string.Empty;

    private readonly Language? language;
    private readonly string languageStr;

    public MediaTrackKind TrackKind { get; }

    internal MediaTrack(MediaTrackKind trackKind, string language = "")
    {
        TrackKind = trackKind;
        languageStr = language;
        Id = string.Empty;
        Label = string.Empty;
    }

    protected MediaTrack(LibVLCSharp.Shared.MediaTrack track)
    {
        TrackKind = Convert(track.TrackType);
        languageStr = track.Language ?? string.Empty;
        if (Windows.Globalization.Language.IsWellFormed(languageStr))
        {
            if (LanguageHelper.TryConvertIso6392ToIso6391(languageStr, out string bc47Tag))
                languageStr = bc47Tag;
            language = new Language(languageStr);
        }

        Id = track.Id.ToString();
        Label = GetFullLabel(track.Description, Language);
    }

    protected MediaTrack(IMediaTrack track)
    {
        languageStr = track.Language;
        if (Windows.Globalization.Language.IsWellFormed(languageStr))
        {
            language = new Language(languageStr);
        }

        Id = track.Id;
        Label = GetFullLabel(track.Label, Language);
    }

    private static string GetFullLabel(string? label, string language)
    {
        if (string.IsNullOrEmpty(label))
        {
            label = language;
        }
        else if (!string.IsNullOrEmpty(language) && language != label)
        {
            label = $"{label} ({language})";
        }

        return label ?? string.Empty;
    }

    private static MediaTrackKind Convert(TrackType trackType)
    {
        return trackType switch
        {
            TrackType.Audio => MediaTrackKind.Audio,
            TrackType.Video => MediaTrackKind.Video,
            TrackType.Text => MediaTrackKind.TimedMetadata,
            _ => throw new ArgumentOutOfRangeException(nameof(trackType), trackType, null)
        };
    }
}
