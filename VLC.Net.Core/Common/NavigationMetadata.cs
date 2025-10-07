#nullable enable

namespace VLC.Net.Core.Common;

internal class NavigationMetadata
{
    public Type RootViewModelType { get; }

    public object? Parameter { get; }

    public NavigationMetadata(Type rootViewModelType, object? parameter)
    {
        RootViewModelType = rootViewModelType;
        Parameter = parameter;
    }
}
