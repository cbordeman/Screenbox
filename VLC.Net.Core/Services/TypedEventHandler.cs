using System.Runtime.InteropServices;

namespace VLC.Net.Core.Services;

public delegate void TypedEventHandler<in TSender, in TResult>([In] TSender sender, [In] TResult args);
