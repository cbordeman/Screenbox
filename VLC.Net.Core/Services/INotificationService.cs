#nullable enable

using VLC.Net.Core.Enums;
using VLC.Net.Core.Events;

namespace VLC.Net.Core.Services
{
    public interface INotificationService
    {
        event EventHandler<NotificationRaisedEventArgs> NotificationRaised;
        event EventHandler<ProgressUpdatedEventArgs> ProgressUpdated;

        void RaiseError(string title, string message);
        void RaiseInfo(string title, string message);
        void RaiseNotification(NotificationLevel level, string title, string message);
        void RaiseWarning(string title, string message);
    }
}