using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;

namespace VLC.Net.Core.Services;

public class AccessListEntry
{
    /// <summary>The identifier of the StorageFile or StorageFolder in the list.</summary>
    public string FilePath;
    /// <summary>Optional app-specified metadata associated with the File or Folder in the list.</summary>
    public string Metadata;
}

public enum AccessCacheOptions
{
    SuppressAccessTimeUpdate
}

public class MostRecentlyUsedService
{
    public ObservableCollection<AccessListEntry> Entries { get; } = new();

    public Task<IStorageFile?> GetFileAsync(string token, AccessCacheOptions suppressAccessTimeUpdate)
    {
        // TODO: Implement suppressAccessTimeUpdate parameter
        
        string? filePath =  Entries.FirstOrDefault(x => x.FilePath == token)?.FilePath;
        return TopLevelDesktopHelper.GetIStorageFileFromPath(filePath);
    }
    
    public void Remove(string token)
    {
        for (int index = 0; index < Entries.Count; index++)
            if (Entries[index].FilePath == token)
                Entries.RemoveAt(index);
    }
}