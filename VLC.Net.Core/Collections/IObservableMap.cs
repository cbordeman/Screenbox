namespace VLC.Net.Core.Collections;

public interface IObservableMap<TKey, TValue> : IDictionary<TKey, TValue>
{
    /// <summary>Occurs when the map changes.</summary>
    event MapChangedEventHandler<TKey, TValue> MapChanged;
}
