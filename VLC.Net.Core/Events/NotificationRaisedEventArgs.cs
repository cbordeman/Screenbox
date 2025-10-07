#nullable enable

using VLC.Net.Core.Enums;

namespace VLC.Net.Core.Events
{
    public sealed class NotificationRaisedEventArgs : EventArgs
    {
        public string Title { get; set; }

        public string Message { get; set; }

        public NotificationLevel Level { get; set; }

        public NotificationRaisedEventArgs(NotificationLevel level, string title, string message)
        {
            Level = level;
            Title = title;
            Message = message;
        }
    }
}
