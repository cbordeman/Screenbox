namespace VLC.Net.Core.Collections;

public interface IMapChangedEventArgs<out TKey>
{
    /// <summary>Gets the type of change that occurred in the map.</summary>
    /// <returns>The type of change in the map.</returns>
    CollectionChange CollectionChange { get; }

    /// <summary>Gets the key of the item that changed.</summary>
    /// <returns>The key of the item that changed.</returns>
    TKey Key { get; }
}
