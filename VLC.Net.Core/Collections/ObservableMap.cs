using System.Collections;

namespace VLC.Net.Core.Collections;

public class ObservableMap<TKey, TValue> : IObservableMap<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> dictionary = new();

    public event MapChangedEventHandler<TKey, TValue>? MapChanged;

    public TValue this[TKey key]
    {
        get => dictionary[key];
        set
        {
            bool exists = dictionary.TryGetValue(key, out var oldValue);
            dictionary[key] = value;
            OnMapChanged(
                exists ? CollectionChange.ItemChanged : CollectionChange.ItemInserted,
                key
            );
        }
    }

    public ICollection<TKey> Keys => dictionary.Keys;
    public ICollection<TValue> Values => dictionary.Values;
    public int Count => dictionary.Count;
    public bool IsReadOnly => false;

    public void Add(TKey key, TValue value)
    {
        dictionary.Add(key, value);
        OnMapChanged(CollectionChange.ItemInserted, key);
    }

    public bool Remove(TKey key)
    {
        if (dictionary.Remove(key))
        {
            OnMapChanged(CollectionChange.ItemRemoved, key);
            return true;
        }
        return false;
    }

    public bool ContainsKey(TKey key) => 
        dictionary.ContainsKey(key);
    public bool TryGetValue(TKey key, out TValue value) => 
        dictionary.TryGetValue(key, out value);
    public void Add(KeyValuePair<TKey, TValue> item) => 
        Add(item.Key, item.Value);

    public void Clear()
    {
        if (dictionary.Count > 0)
        {
            dictionary.Clear();
            OnMapChanged(CollectionChange.Reset, default!);
        }
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) => 
        dictionary.ContainsKey(item.Key) &&
        EqualityComparer<TValue>.Default.Equals(
            dictionary[item.Key], item.Value);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
        ((IDictionary<TKey, TValue>)dictionary).CopyTo(array, arrayIndex);

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        if (Contains(item) && dictionary.Remove(item.Key))
        {
            OnMapChanged(CollectionChange.ItemRemoved, item.Key);
            return true;
        }
        return false;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dictionary.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void OnMapChanged(CollectionChange change, TKey key)
    {
        MapChanged?.Invoke(
            this,
            new MapChangedEventArgs<TKey>(change, key)
        );
    }
}
