using System.Runtime.InteropServices;
using Avalonia.Platform.Storage;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.Infrastructure;

public interface IStorageItemQueryResult : IStorageQueryResultBase
{
    Task<IReadOnlyList<IStorageItem>> GetItemsAsync([In] uint startIndex, [In] uint maxNumberOfItems);
    Task<IReadOnlyList<IStorageItem>> GetItemsAsync();
}

public sealed class StorageItemQueryResult : IStorageItemQueryResult
{
  /// <summary>Retrieves a list of items (files and folders) in a specified range.</summary>
  /// <param name="startIndex">The zero-based index of the first item to retrieve. This parameter defaults to 0.</param>
  /// <param name="maxNumberOfItems">The maximum number of items to retrieve. Use -1 to retrieve all items. If the range contains fewer items than the max number, all items in the range are returned.</param>
  /// <returns>When this method completes successfully, it returns a list (type IVectorView ) of items. Each item is the IStorageItem type and represents a file, folder, or file group.</returns>
  public extern Task<IReadOnlyList<IStorageItem>> GetItemsAsync(
    [In] uint startIndex,
    [In] uint maxNumberOfItems);
  /// <summary>Retrieves a list of all the items (files and folders) in the query results set.</summary>
  /// <returns>When this method completes successfully, it returns a list (type IVectorView ) of items. Each item is the IStorageItem type and represents a file, folder, or file group.</returns>
  public extern Task<IReadOnlyList<IStorageItem>> GetItemsAsync();
  /// <summary>Retrieves the number of items in the set of query results.</summary>
  /// <returns>When this method completes successfully, it returns the number of items in the location that match the query.</returns>
  public extern Task<uint> GetItemCountAsync();
  /// <summary>Gets the folder originally used to create the StorageItemQueryResult object. This folder represents the scope of the query.</summary>
  /// <returns>The original folder.</returns>
  public extern IStorageFolder Folder
  {
      get;
  }
  /// <summary>Fires when an item is added to, deleted from, or modified in the folder being queried. This event only fires after GetItemsAsync has been called at least once.</summary>
  public extern event TypedEventHandler<IStorageQueryResultBase, object> ContentsChanged;
  /// <summary>Fires when the query options change.</summary>
  public extern event TypedEventHandler<IStorageQueryResultBase, object> OptionsChanged;
  /// <summary>Retrieves the index of the item from the query results that most closely matches the specified property value. The property that is matched is determined by the first SortEntry of the QueryOptions.SortOrder list.</summary>
  /// <param name="value">The property value to match when searching the query results. The property to that is used to match this value is the property in the first SortEntry of the QueryOptions.SortOrder list.</param>
  /// <returns>When this method completes successfully it returns the index of the matched item in the query results.</returns>
  public extern Task<uint> FindStartIndexAsync([In] object value);
  /// <summary>Retrieves the query options used to determine query results.</summary>
  /// <returns>The query options.</returns>
  public extern QueryOptions GetCurrentQueryOptions();
  /// <summary>Modifies query results based on new QueryOptions.</summary>
  /// <param name="newQueryOptions">The new query options.</param>
  public extern void ApplyNewQueryOptions([In] QueryOptions newQueryOptions);
}


public interface IStorageFileQueryResult : IStorageQueryResultBase
{
    Task<IReadOnlyList<IStorageFile>> GetFilesAsync([In] uint startIndex, [In] uint maxNumberOfItems);
    Task<IReadOnlyList<IStorageFile>> GetFilesAsync();
}

public interface IStorageQueryResultBase
{
    /// <summary>Retrieves the number of items that match the query that created a StorageFileQueryResult, StorageFolderQueryResult, or StorageItemQueryResult object.</summary>
    /// <returns>When this method completes successfully, it returns the number of items that match the query.</returns>
    Task<uint> GetItemCountAsync();
    /// <summary>Gets the folder originally used to create a StorageFileQueryResult, StorageFolderQueryResult, or StorageItemQueryResult object. This folder represents the scope of the query.</summary>
    /// <returns>The original folder.</returns>
    IStorageFolder Folder { get; }
    /// <summary>Fires when an item is added, deleted or modified in the folder being queried.</summary>
    event TypedEventHandler<IStorageQueryResultBase, object> ContentsChanged;
    /// <summary>Fires when the query options are changed for a StorageFileQueryResult, StorageFolderQueryResult, or StorageItemQueryResult object.</summary>
    event TypedEventHandler<IStorageQueryResultBase, object> OptionsChanged;
    /// <summary>Retrieves the index of the file from the query results that most closely matches the specified property value. The property that is matched is determined by the first SortEntry of the QueryOptions.SortOrder list.</summary>
    /// <param name="value">The property value to match when searching the query results.</param>
    /// <returns>When this method completes successfully it returns the index of the matched item in the query results.</returns>
    Task<uint> FindStartIndexAsync([In] object value);
    /// <summary>Retrieves the query options used to create a StorageFileQueryResult, StorageFolderQueryResult, or StorageItemQueryResult object.</summary>
    /// <returns>The query options.</returns>
    QueryOptions GetCurrentQueryOptions();
    /// <summary>Applies new query options to the results retrieved by the StorageFileQueryResult, StorageFolderQueryResult, or StorageItemQueryResult object.</summary>
    /// <param name="newQueryOptions">The new query options.</param>
    void ApplyNewQueryOptions([In] QueryOptions newQueryOptions);
}

public struct TextSegment
{
    /// <summary>The zero-based index of the start of the associated text segment.</summary>
    public uint StartPosition;
    /// <summary>The number of characters in the associated text segment.</summary>
    public uint Length;
}

public interface IStorageFileQueryResult2 : IStorageQueryResultBase
{
    IDictionary<string, IReadOnlyList<TextSegment>> GetMatchingPropertiesWithRanges([In] IStorageFile file);
}

public sealed class StorageFileQueryResult :
    IStorageFileQueryResult,
    IStorageFileQueryResult2
{
    /// <summary>Retrieves a list of files in a specified range.</summary>
    /// <param name="startIndex">The zero-based index of the first file to retrieve. This parameter is 0 by default.</param>
    /// <param name="maxNumberOfItems">The maximum number of files to retrieve. Use -1 to retrieve all files. If the range contains fewer files than the max number, all files in the range are returned.</param>
    /// <returns>When this method completes successfully, it returns a list (type IVectorView ) of files that are represented by storageFile objects.</returns>
    public extern Task<IReadOnlyList<IStorageFile>> GetFilesAsync(
        [In] uint startIndex,
        [In] uint maxNumberOfItems);
    /// <summary>Retrieves a list of all the files in the query result set.</summary>
    /// <returns>When this method completes successfully, it returns a list (type IVectorView ) of files that are represented by storageFile objects.</returns>
    public extern Task<IReadOnlyList<IStorageFile>> GetFilesAsync();
    /// <summary>Retrieves the number of files in the set of query results.</summary>
    /// <returns>When this method completes successfully, it returns the number of files in the location that match the query.</returns>
    public extern Task<uint> GetItemCountAsync();
    /// <summary>Gets the folder that was queried to create the StorageFileQueryResult object. This folder represents the scope of the query.</summary>
    /// <returns>The original folder.</returns>
    public extern IStorageFolder Folder
    {
        get;
    }
    /// <summary>Fires when a file is added to, deleted from, or modified in the folder being queried. This event only fires after GetFilesAsync has been called at least once.</summary>
    public extern event TypedEventHandler<IStorageQueryResultBase, object> ContentsChanged;
    /// <summary>Fires when the query options change.</summary>
    public extern event TypedEventHandler<IStorageQueryResultBase, object> OptionsChanged;
    /// <summary>Retrieves the index of the file from the query results that most closely matches the specified property value (or file, if used with FileActivatedEventArgs.NeighboringFilesQuery ). The property that is matched is determined by the first SortEntry of the QueryOptions.SortOrder list.</summary>
    /// <param name="value">The property value to match when searching the query results. The property to that is used to match this value is the property in the first SortEntry of the QueryOptions.SortOrder list.</param>
    /// <returns>When this method completes successfully, it returns the index of the matched file in the query results or the index of the file in the FileActivatedEventArgs.NeighboringFilesQuery. In the latter case, the file is expected to be sourced from FileActivatedEventArgs.Files. If this function fails, it returns **uint.MaxValue**.</returns>
    public extern Task<uint> FindStartIndexAsync([In] object value);
    /// <summary>Retrieves the query options used to determine query results.</summary>
    /// <returns>The query options.</returns>
    public extern QueryOptions GetCurrentQueryOptions();
    /// <summary>Modifies query results based on new QueryOptions.</summary>
    /// <param name="newQueryOptions">The new query options.</param>
    public extern void ApplyNewQueryOptions([In] QueryOptions newQueryOptions);
    /// <summary>Gets matching file properties with corresponding text ranges.</summary>
    /// <param name="file">The file to query for properties.</param>
    /// <returns>The matched properties and corresponding text ranges.</returns>
    public extern IDictionary<string, IReadOnlyList<TextSegment>> GetMatchingPropertiesWithRanges(
        [In] IStorageFile file);
}
