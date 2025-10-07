#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using VLC.Net.Core.Messages;
using VLC.Net.Core.Playback;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class VolumeViewModel : ObservableRecipient,
        IRecipient<ChangeVolumeRequestMessage>,
        IRecipient<SettingsChangedMessage>,
        IRecipient<MediaPlayerChangedMessage>
    {
        [ObservableProperty] private int maxVolume;
        [ObservableProperty] private int volume;
        [ObservableProperty] private bool isMute;
        private IMediaPlayer? mediaPlayer;
        private readonly DispatcherQueue dispatcherQueue;
        private readonly ISettingsService settingsService;

        public VolumeViewModel(ISettingsService settingsService)
        {
            this.settingsService = settingsService;
            volume = settingsService.PersistentVolume;
            maxVolume = settingsService.MaxVolume;
            isMute = volume == 0;
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // View model doesn't receive any messages
            IsActive = true;
        }

        public void Receive(MediaPlayerChangedMessage message)
        {
            mediaPlayer = message.Value;
            mediaPlayer.VolumeChanged += OnVolumeChanged;
            mediaPlayer.IsMutedChanged += OnIsMutedChanged;
        }

        public void Receive(SettingsChangedMessage message)
        {
            if (message.SettingsName != nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.VolumeBoost)) return;
            MaxVolume = settingsService.MaxVolume;
        }

        public void Receive(ChangeVolumeRequestMessage message)
        {
            SetVolume(message.Value, message.IsOffset);
            message.Reply(Volume);
        }

        partial void OnVolumeChanged(int value)
        {
            if (mediaPlayer == null) return;
            double newValue = value / 100d;
            // bool stayMute = IsMute && newValue - _mediaPlayer.Volume < 0.005;
            mediaPlayer.Volume = newValue;
            if (value > 0) IsMute = false;
            settingsService.PersistentVolume = value;
        }

        partial void OnIsMuteChanged(bool value)
        {
            if (mediaPlayer == null) return;
            mediaPlayer.IsMuted = value;
        }

        private void OnVolumeChanged(IMediaPlayer sender, object? args)
        {
            double normalizedVolume = Volume / 100d;
            if (Math.Abs(sender.Volume - normalizedVolume) > 0.001)
            {
                dispatcherQueue.TryEnqueue(() => sender.Volume = normalizedVolume);
            }
        }

        private void OnIsMutedChanged(IMediaPlayer sender, object? args)
        {
            if (sender.IsMuted != IsMute)
            {
                dispatcherQueue.TryEnqueue(() => sender.IsMuted = IsMute);
            }
        }

        /// <summary>
        /// Sets the volume to a specified value or adjusts it by a given amount.
        /// </summary>
        /// <param name="value">The target volume to set or the offset amount to adjust.</param>
        /// <param name="isOffset">If <see langword="true"/>, adjusts the current volume by the specified <paramref name="value"/>;
        /// otherwise, sets the volume directly. The default value is <see langword="false"/>.</param>
        public void SetVolume(int value, bool isOffset = false)
        {
            Volume = Math.Clamp((int)(isOffset ? Volume + value : value), (int)0, (int)MaxVolume);
        }
    }
}
