#nullable enable

using MediaViewModel = VLC.Net.Core.ViewModels.MediaViewModel;

namespace VLC.Net.Core.Services
{
    public interface ISystemMediaTransportControlsService
    {
        SystemMediaTransportControls TransportControls { get; }
        Task UpdateTransportControlsDisplayAsync(MediaViewModel? item);
        void UpdatePlaybackPosition(TimeSpan position, TimeSpan startTime, TimeSpan endTime);
        void UpdatePlaybackStatus(MediaPlaybackState state);
        void ClosePlayback();
    }
}