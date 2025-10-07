namespace VLC.Net.Core.Services;

public record MruEntry(string Path, string Name);

public class MostRecentlyUsedService
{
    public List<MruEntry> Entries { get; } = new();
}
