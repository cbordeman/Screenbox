#nullable enable

namespace VLC.Net.Core.Common
{
    public interface IContentFrame
    {
        object? FrameContent { get; }
        Type ContentSourcePageType { get; }
        bool CanGoBack { get; }
        void GoBack();
        void NavigateContent(Type pageType, object? parameter);
    }
}
