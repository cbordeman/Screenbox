using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VLC.Net.Core.Enums;

namespace VLC.Net.Core.Infrastructure;

public enum CommonFolderQuery
{
  /// <summary>A shallow list of the folders in the current folder, similar to the view that File Explorer provides.</summary>
  DefaultQuery = 0,
  /// <summary>Group files into virtual folders by year based on the System.ItemDate property of each file. Each folder will contain all (and only) files that have values for System.ItemDate within the same year.</summary>
  GroupByYear = 100,
  /// <summary>Group files into virtual folders by month based on the System.ItemDate property of each file. Each folder will contain all (and only) files that have values for System.ItemDate within the same month.</summary>
  GroupByMonth = 101,
  /// <summary>Group files into virtual folders based on the System.Music.Artist property of each file. Each folder will contain all (and only) files with identical values for System.Music.Artist.</summary>
  GroupByArtist = 102,
  /// <summary>Group files into virtual folders by year based on the System.Music.AlbumTitle property of each file. Each folder will contain all (and only) files with identical values for System.Music.AlbumTitle.</summary>
  GroupByAlbum = 103,
  /// <summary>Group files into virtual folders based on the System.Music.AlbumArtist property of each file. Each folder will contain all (and only) files with identical values for System.Music.AlbumArtist.</summary>
  GroupByAlbumArtist = 104,
  /// <summary>Group files into virtual folders based on the System.Music.Composer property of each file. Each folder will represent one composer, and contain all files whose System.Music.Composer vector contains that composer. If a file lists multiple composers, it may appear in more than one of the resulting folders.</summary>
  GroupByComposer = 105,
  /// <summary>Group files into virtual folders based on the System.Music.Genre property of each file. Each folder will contain all (and only) files with identical values for System.Music.Genre.</summary>
  GroupByGenre = 106,
  /// <summary>Group files into virtual folders by year based on the System.Media.Year property of each file. Each folder will contain all (and only) files that have values for System.Media.Year within the same year.</summary>
  GroupByPublishedYear = 107,
  /// <summary>Group files into virtual folders by rating (1 star, 2 stars, and so on) based on the System.Rating property of each file. Each folder will contain all (and only) files with identical values for System.Rating.</summary>
  GroupByRating = 108,
  /// <summary>Group files into virtual folders based on the System.Keywords property of each file. Each folder will represent one tag, and contain all files whose System.Keywords vector contains that tag. If a file lists multiple tags in its System.Keywords vector, it may appear in more than one of the resulting folders.</summary>
  GroupByTag = 109,
  /// <summary>Group files into virtual folders based on the System.Author property of each file. Each folder will represent one author, and contain all files whose System.Author vector contains that author. If a file lists multiple authors, it may appear in more than one of the resulting folders.</summary>
  GroupByAuthor = 110,
  /// <summary>Group files into virtual folders by type (for example, Microsoft Word documents, text files, and so forth) based on the System.ItemTypeText property of each file.</summary>
  GroupByType = 111,
}

public interface IQueryOptionsWithProviderFilter
{
    IList<string> StorageProviderIdFilter { get; }
}

public enum PropertyPrefetchOptions : uint
{
    /// <summary>No specific, system-defined property group.</summary>
    None = 0U,
    /// <summary>A group of music-related properties that can be access through a MusicProperties object.</summary>
    MusicProperties = 1U,
    /// <summary>A group of video-related properties that can be access through a VideoProperties object.</summary>
    VideoProperties = 2U,
    /// <summary>A group of image-related properties that can be access through a ImageProperties object.</summary>
    ImageProperties = 4U,
    /// <summary>A group of document-related properties that can be access through a DocumentProperties object.</summary>
    DocumentProperties = 8U,
    /// <summary>A group of basic properties that can be access through a BasicProperties object.</summary>
    BasicProperties = 16U,
}

public enum ThumbnailOptions : uint
{
    /// <summary>No options.</summary>
    None = 0U,
    /// <summary>Retrieve a thumbnail only if it is cached or embedded in the file.</summary>
    ReturnOnlyIfCached = 1U,
    /// <summary>Scale the thumbnail to the requested size.</summary>
    ResizeThumbnail = 2U,
    /// <summary>Default. Increase requested size based on the Pixels Per Inch (PPI) of the display.</summary>
    UseCurrentScale = 4U,
}

public enum ThumbnailMode
{
    /// <summary>To display previews of picture files.</summary>
    PicturesView,
    /// <summary>To display previews of video files.</summary>
    VideosView,
    /// <summary>To display previews of music files.</summary>
    MusicView,
    /// <summary>To display previews of document files.</summary>
    DocumentsView,
    /// <summary>To display previews of files (or other items) in a list.</summary>
    ListView,
    /// <summary>To display a preview of any single item (like a file, folder, or file group).</summary>
    SingleItem,
}

public enum DateStackOption
{
    /// <summary>The query options are not based on the date.</summary>
    None,
    /// <summary>The content is grouped by year.</summary>
    Year,
    /// <summary>The content is grouped by month.</summary>
    Month,
}

public struct SortEntry
{
    /// <summary>The name of the property to use for sorting, like System.Author. The property must be registered on the system.</summary>
    public string PropertyName;
    /// <summary>True to sort content in the query results in ascending order based on the property name, or false to sort content in descending order.</summary>
    public bool AscendingOrder;
}

public enum FolderDepth
{
    /// <summary>Perform a shallow enumeration of the folder being queried. Only the top-level, child files and folders of the folder being queried will be returned. This is similar to the view that Windows Explorer would provide for the folder being queried.</summary>
    Shallow,
    /// <summary>Perform a deep enumeration of the folder contents. Windows traverses through subfolders to discover content and presents the results in a single list that combines all discovered content.</summary>
    Deep,
}

public enum IndexerOption
{
    /// <summary>Use the system index for content that has been indexed and use the file system directly for content that has not been indexed.</summary>
    UseIndexerWhenAvailable,
    /// <summary>Use only indexed content and ignore content that has not been indexed.</summary>
    OnlyUseIndexer,
    /// <summary>Access the file system directly; don't use the system index.</summary>
    DoNotUseIndexer,
    /// <summary>Only returns the properties specified in QueryOptions.SetPropertyPrefetch for faster results.</summary>
    OnlyUseIndexerAndOptimizeForIndexedProperties,
}

public interface IQueryOptions
{
    IList<string> FileTypeFilter { get; }
    FolderDepth FolderDepth { get; [param: In] set; }
    string ApplicationSearchFilter { get; [param: In] set; }
    string UserSearchFilter { get; [param: In] set; }
    string Language { get; [param: In] set; }
    IndexerOption IndexerOption { get; [param: In] set; }
    IList<SortEntry> SortOrder { get; }
    string GroupPropertyName { get; }
    DateStackOption DateStackOption { get; }
    string SaveToString();
    void LoadFromString([In] string value);
    void SetThumbnailPrefetch([In] ThumbnailMode mode, [In] uint requestedSize, [In] ThumbnailOptions options);
    void SetPropertyPrefetch(
        [In] PropertyPrefetchOptions options,
        [In] IEnumerable<string> propertiesToRetrieve);
}

public sealed class QueryOptions : IQueryOptions, IQueryOptionsWithProviderFilter
{
    public extern QueryOptions([In] CommonFileQuery query, [In] IEnumerable<string> fileTypeFilter);
    /// <summary>Creates an instance of the QueryOptions class for enumerating subfolders and initializes it with values based on the specified CommonFolderQuery.</summary>
    /// <param name="query">An enumeration value that specifies how to group the contents of the folder where the query is created into subfolders to enumerate. The subfolders that are retrieved using a CommonFolderQuery can be actual file system folders or virtual folders that represent groups of files (which are determined by the CommonFolderQuery value).</param>
    public extern QueryOptions([In] CommonFolderQuery query);
    /// <summary>Creates an instance of the QueryOptions class for enumerating storage items, and initializes it with the following default settings: QueryOptions.FolderDepth gets FolderDepth.Shallow and QueryOptions.IndexerOption gets IndexerOption.DoNotUseIndexer.</summary>
    public extern QueryOptions();
    /// <summary>Gets a list of file name extensions used to filter the search results. If the list is empty, the results include all file types.</summary>
    /// <returns>The list of file types of files include in query results. The default value is an empty list (which is equivalent to a list containing only "*") that includes all file types.</returns>
    public extern IList<string> FileTypeFilter
    {
        [MethodImpl(MethodCodeType = MethodCodeType.Runtime)]
        get;
    }
    /// <summary>Indicates whether the search query should produce a shallow view of the folder contents or a deep recursive view of all files and subfolder.</summary>
    /// <returns>A value that indicates how deeply to query the folder. The default value is FolderDepth.Shallow. Predefined queries typically override this property and change it to FolderDepth.Deep.</returns>
    public extern FolderDepth FolderDepth
    {
        [MethodImpl(MethodCodeType = MethodCodeType.Runtime)]
        get;
        [MethodImpl(MethodCodeType = MethodCodeType.Runtime)] [param: In]
        set;
    }
    /// <summary>Gets or sets an application-defined Advanced Query Syntax (AQS) string for filtering files by keywords or properties. This property is combined with the UserSearchFilter to create the query's search filter.</summary>
    /// <returns>A simple keyword, or a string that conforms to Advanced Query Syntax (AQS). For more information, see Using Advanced Query Syntax Programmatically.</returns>
    public extern string ApplicationSearchFilter
    {
        [MethodImpl(MethodCodeType = MethodCodeType.Runtime)]
        get;
        [MethodImpl(MethodCodeType = MethodCodeType.Runtime)] [param: In]
        set;
    }
    /// <summary>Gets or sets a user-defined Advanced Query Syntax (AQS) string for filtering files by keywords or properties. This property is combined with the ApplicationSearchFilter to create the query's search filter.</summary>
    /// <returns>A simple keyword or a string that conforms to Advanced Query Syntax (AQS). For more information, see Using Advanced Query Syntax Programmatically.</returns>
    public extern string UserSearchFilter
    {
        [MethodImpl(MethodCodeType = MethodCodeType.Runtime)]
        get;
        [MethodImpl(MethodCodeType = MethodCodeType.Runtime)] [param: In]
        set;
    }
    /// <summary>Gets or sets the Internet Engineering Task Force (IETF) language tag (BCP47 standard) that identifies the language associated with the query. This determines the language-specific algorithm used by the system to break the query into individual search tokens.</summary>
    /// <returns>The Internet Engineering Task Force (IETF) BCP47-standard language tag.</returns>
    public extern string Language
    {
        [MethodImpl(MethodCodeType = MethodCodeType.Runtime)]
        get;
        [MethodImpl(MethodCodeType = MethodCodeType.Runtime)] [param: In]
        set;
    }
    /// <summary>Gets or sets a value that specifies whether the system index or the file system is used to retrieve query results. The indexer can retrieve results faster but is not available in all file locations.</summary>
    /// <returns>The indexer option.</returns>
    public extern IndexerOption IndexerOption
    {
        [MethodImpl(MethodCodeType = MethodCodeType.Runtime)]
        get;
        [MethodImpl(MethodCodeType = MethodCodeType.Runtime)] [param: In]
        set;
    }
    /// <summary>Gets the list of SortEntry structures that specify how to sort content (like files and subfolders) in query results. Use this list to customize how query results are sorted.</summary>
    /// <returns>A SortEntryVector that contains SortEntry structures. These structures specify how to sort query results.</returns>
    public extern IList<SortEntry> SortOrder
    {
        [MethodImpl(MethodCodeType = MethodCodeType.Runtime)]
        get;
    }
    /// <summary>Gets the name of the property used to group query results if the QueryOptions object was created using a CommonFolderQuery. For example, if CommonFolderQuery.GroupByYear is used to create a QueryOptions object, the value of this property is System.ItemDate.</summary>
    /// <returns>The property that is being used to group files and that is specified by the CommonFolderQuery enumeration.</returns>
    public extern string GroupPropertyName
    {
        [MethodImpl(MethodCodeType = MethodCodeType.Runtime)]
        get;
    }
    /// <summary>Gets the unit of time used to group files into folders if the QueryOptions object was created with a CommonFolderQuery based on date. For example, if CommonFolderQuery.GroupByYear is used to create a QueryOptions object, the value of this property is DateStackOption.Year.</summary>
    /// <returns>The unit of time user to group folder content by date.</returns>
    public extern DateStackOption DateStackOption
    {
        [MethodImpl(MethodCodeType = MethodCodeType.Runtime)]
        get;
    }
    /// <summary>Converts the values of a QueryOptions object to a string that can be used to initialize the values of a QueryOptions object by calling LoadFromString.</summary>
    /// <returns>A string representing the serialized settings of a QueryOptions instance.</returns>
    public extern string SaveToString();
    /// <summary>Initializes the current instance of the QueryOptions class with search parameters specified by a string that was created by the SaveToString method.</summary>
    /// <param name="value">A string retrieved by a previous call to SaveToString.</param>
    public extern void LoadFromString([In] string value);
    /// <summary>Specifies the type and size of thumbnails that the system should start loading immediately when items are accessed (instead of retrieving them on a case-by-case basis). This uses more resources but makes thumbnail retrieval on query results faster.</summary>
    /// <param name="mode">The enumeration value that describes the purpose of the thumbnail and determines how the thumbnail image is adjusted.</param>
    /// <param name="requestedSize">The requested size, in pixels, of the longest edge of the thumbnail. Windows uses the *requestedSize* as a guide and tries to return a thumbnail image that can be scaled to the requested size without reducing the quality of the image.</param>
    /// <param name="options">The enum value that describes the desired behavior to use to retrieve the thumbnail image. The specified behavior might affect the size and/or quality of the image and how quickly the thumbnail image is retrieved.</param>
    public extern void SetThumbnailPrefetch(
        [In] ThumbnailMode mode,
        [In] uint requestedSize,
        [In] ThumbnailOptions options);
    [MethodImpl(MethodCodeType = MethodCodeType.Runtime)]
    public extern void SetPropertyPrefetch(
        [In] PropertyPrefetchOptions options,
        [In] IEnumerable<string> propertiesToRetrieve);
    /// <summary>Gets the filter for storage provider identifiers.</summary>
    /// <returns>The filter string.</returns>
    public extern IList<string> StorageProviderIdFilter
    {
        [MethodImpl(MethodCodeType = MethodCodeType.Runtime)]
        get;
    }
}