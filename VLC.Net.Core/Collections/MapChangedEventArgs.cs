namespace VLC.Net.Core.Collections;

public class MapChangedEventArgs<TKey> : IMapChangedEventArgs<TKey>
{
    public MapChangedEventArgs(CollectionChange change, TKey key)
    {
        CollectionChange = change;
        Key = key;
    }

    public CollectionChange CollectionChange { get; }
    public TKey Key { get; }
}
