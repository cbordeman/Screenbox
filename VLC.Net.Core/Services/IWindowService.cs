#nullable enable

using VLC.Net.Core.Enums;
using VLC.Net.Core.Events;

namespace VLC.Net.Core.Services
{
    public interface IWindowService
    {
        public event EventHandler<ViewModeChangedEventArgs>? ViewModeChanged;
        public WindowViewMode ViewMode { get; }
        public bool TryEnterFullScreen();
        public void ExitFullScreen();
        public Task<bool> TryExitCompactLayoutAsync();
        public Task<bool> TryEnterCompactLayoutAsync(Size viewSize);
        Size GetMaxWindowSize();
        double ResizeWindow(Size desiredSize, double scalar = 1);
        void HideCursor();
        void ShowCursor();
    }
}