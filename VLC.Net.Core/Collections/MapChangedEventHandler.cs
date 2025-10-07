using System.Runtime.InteropServices;

namespace VLC.Net.Core.Collections;

public delegate void MapChangedEventHandler<TKey, TValue>(
    [In] IObservableMap<TKey, TValue> sender,
    [In] IMapChangedEventArgs<TKey> @event);
