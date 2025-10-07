using VLC.Net.Core.Enums;

namespace VLC.Net.Core.Events
{
    public sealed class ViewModeChangedEventArgs : ValueChangedEventArgs<WindowViewMode>
    {
        public ViewModeChangedEventArgs(WindowViewMode newValue, WindowViewMode oldValue) : base(newValue, oldValue)
        {
        }
    }
}
