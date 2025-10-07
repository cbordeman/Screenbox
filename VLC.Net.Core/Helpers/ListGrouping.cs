namespace VLC.Net.Core.Helpers;
internal sealed class ListGrouping<TKey, TValue> : List<TValue>, IGrouping<TKey, TValue>
{
    public TKey Key { get; }

    public ListGrouping(TKey key)
    {
        Key = key;
    }

    public ListGrouping(TKey key, IEnumerable<TValue> list) : base(list)
    {
        Key = key;
    }
}
