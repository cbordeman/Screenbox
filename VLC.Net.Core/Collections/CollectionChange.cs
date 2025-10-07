namespace VLC.Net.Core.Collections;

public enum CollectionChange
{
    /// <summary>The collection is changed.</summary>
    Reset,
    /// <summary>An item is added to the collection.</summary>
    ItemInserted,
    /// <summary>An item is removed from the collection.</summary>
    ItemRemoved,
    /// <summary>An item is changed in the collection.</summary>
    ItemChanged,
}
