#nullable enable

using LibVLCSharp.Shared;
using VLC.Net.Core.Events;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace VLC.Net.Core.Playback
{
    public sealed class VlcMediaPlayer : IMediaPlayer
    {
        public event TypedEventHandler<IMediaPlayer, EventArgs>? MediaEnded;
        public event TypedEventHandler<IMediaPlayer, EventArgs>? MediaFailed;
        public event TypedEventHandler<IMediaPlayer, EventArgs>? MediaOpened;
        public event TypedEventHandler<IMediaPlayer, EventArgs>? IsMutedChanged;
        public event TypedEventHandler<IMediaPlayer, EventArgs>? VolumeChanged;
        public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<PlaybackItem?>>? PlaybackItemChanged;
        public event TypedEventHandler<IMediaPlayer, EventArgs>? BufferingProgressChanged;
        public event TypedEventHandler<IMediaPlayer, EventArgs>? BufferingStarted;
        public event TypedEventHandler<IMediaPlayer, EventArgs>? BufferingEnded;
        public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<TimeSpan>>? NaturalDurationChanged;
        public event TypedEventHandler<IMediaPlayer, EventArgs>? NaturalVideoSizeChanged;
        public event TypedEventHandler<IMediaPlayer, EventArgs>? CanSeekChanged;
        public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<TimeSpan>>? PositionChanged;
        public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<ChapterCue?>>? ChapterChanged;
        public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<MediaPlaybackState>>? PlaybackStateChanged;
        public event TypedEventHandler<IMediaPlayer, ValueChangedEventArgs<double>>? PlaybackRateChanged;

        public ChapterCue? Chapter
        {
            get => chapter;
            set
            {
                if (value == chapter) return;
                ChapterCue? oldValue = chapter;
                chapter = value;
                ChapterChanged?.Invoke(this, new ValueChangedEventArgs<ChapterCue?>(value, oldValue));
            }
        }

        public TimeSpan NaturalDuration
        {
            get => naturalDuration;
            private set
            {
                // Length can fluctuate during playback. Check for tolerance here.
                if (Math.Abs((naturalDuration - value).TotalMilliseconds) <= 50) return;
                TimeSpan oldValue = naturalDuration;
                naturalDuration = value;
                NaturalDurationChanged?.Invoke(this, new ValueChangedEventArgs<TimeSpan>(value, oldValue));
            }
        }

        public TimeSpan Position
        {
            get => position;
            set
            {
                if (VlcPlayer.Length < 0) return;
                if (value < TimeSpan.Zero) value = TimeSpan.Zero;
                if (value > NaturalDuration) value = NaturalDuration;
                if (VlcPlayer.State == VLCState.Ended)
                {
                    if (value == NaturalDuration)
                    {
                        position = value;
                        return;
                    }

                    Replay();
                }

                TimeSpan oldValue = position;
                position = value;
                long ms = (long)value.TotalMilliseconds;
                VlcPlayer.Time = ms;
                // Position changed will not fire if the player is paused
                // TODO: Check for more problematic states
                if (PlaybackState is MediaPlaybackState.Paused)
                {
                    PositionChanged?.Invoke(this, new ValueChangedEventArgs<TimeSpan>(value, oldValue));
                }
            }
        }

        public bool IsMuted
        {
            get => VlcPlayer.Mute;
            set
            {
                if (VlcPlayer.Mute != value)
                {
                    VlcPlayer.Mute = value;
                }
            }
        }

        public double Volume
        {
            get => VlcPlayer.Volume / 100d;
            set
            {
                int iVal = (int)(value * 100);
                if (VlcPlayer.Volume != iVal && VlcPlayer.Volume >= 0)
                {
                    VlcPlayer.Volume = iVal;
                }
            }
        }

        public double PlaybackRate
        {
            get => VlcPlayer.Rate;
            set
            {
                if (Math.Abs(VlcPlayer.Rate - value) > 0.0001)
                {
                    double oldValue = VlcPlayer.Rate;
                    VlcPlayer.SetRate((float)value);
                    PlaybackRateChanged?.Invoke(this, new ValueChangedEventArgs<double>(value, oldValue));
                }
            }
        }

        public Rect NormalizedSourceRect
        {
            get => normalizedSourceRect;
            set
            {
                if (value == defaultSourceRect)
                {
                    normalizedSourceRect = defaultSourceRect;
                    VlcPlayer.CropGeometry = null;
                }
                else
                {
                    normalizedSourceRect = value;

                    /*
                    This is how CropGeometry is parsed. Note that it only takes integer inputs.
                    https://code.videolan.org/videolan/vlc/-/blob/56222b9290dd9bf08e02b10b1e9ee13d68931fc2/src/video_output/vout_intf.c#L452-464

                    if (sscanf(newval.psz_string, "%u:%u", &num, &den) == 2) {
                       vout_ControlChangeCropRatio(vout, num, den);
                    } else if (sscanf(newval.psz_string, "%ux%u+%u+%u", &width, &height, &x, &y) == 4) {
                       vout_ControlChangeCropWindow(vout, x, y, width, height);
                    } else if (sscanf(newval.psz_string, "%u+%u+%u+%u", &left, &top, &right, &bottom) == 4) {
                       vout_ControlChangeCropBorder(vout, left, top, right, bottom);
                    } else if (*newval.psz_string == '\0') {
                       vout_ControlChangeCropRatio(vout, 0, 0);
                    } else {
                       msg_Err(object, "Unknown crop format (%s)", newval.psz_string);
                    }
                    */

                    // double rightOffset = value.Right * NaturalVideoWidth;
                    // double bottomOffset = value.Bottom * NaturalVideoHeight;
                    // double leftOffset = value.Left * NaturalVideoWidth;
                    // double topOffset = value.Top * NaturalVideoHeight;
                    // VlcPlayer.CropGeometry = $"{rightOffset:F0}x{bottomOffset:F0}+{leftOffset:F0}+{topOffset:F0}";

                    // Use crop ratio to avoid conflict with subtitle rendering
                    double newWidth = value.Width * NaturalVideoWidth;
                    double newHeight = value.Height * NaturalVideoHeight;
                    VlcPlayer.CropGeometry = $"{newWidth:F0}:{newHeight:F0}";
                }
            }
        }

        public DeviceInformation? AudioDevice
        {
            get => null;    // TODO: Implement AudioDevice getter
            set
            {
                string? deviceId = value?.Id;
                deviceId ??= VlcPlayer.OutputDevice;
                if (deviceId == null) return;
                VlcPlayer.SetOutputDevice(deviceId);
            }
        }

        public MediaPlaybackState PlaybackState
        {
            get => playbackState;
            private set
            {
                if (value == playbackState) return;
                MediaPlaybackState oldValue = playbackState;
                playbackState = value;
                PlaybackStateChanged?.Invoke(this, new ValueChangedEventArgs<MediaPlaybackState>(value, oldValue));
            }
        }

        public PlaybackItem? PlaybackItem
        {
            get => playbackItem;
            set
            {
                if (playbackItem == value) return;
                PlaybackItem? oldValue = playbackItem;
                if (value == null)
                {
                    VlcPlayer.Stop();
                    if (playbackItem != null) RemoveItemHandlers(playbackItem);
                    playbackItem = null;
                }
                else
                {
                    playbackItem = value;
                    RegisterItemHandlers(playbackItem);
                    readyToPlay = true;
                    updateMediaProperties = true;
                }

                PlaybackItemChanged?.Invoke(this, new ValueChangedEventArgs<PlaybackItem?>(value, oldValue));
            }
        }

        public bool CanSeek { get; private set; }

        public bool IsLoopingEnabled { get; set; }

        public double BufferingProgress { get; private set; }

        public uint NaturalVideoHeight { get; private set; }

        public uint NaturalVideoWidth { get; private set; }

        public bool CanPause => VlcPlayer.CanPause;

        internal MediaPlayer VlcPlayer { get; }

        internal LibVLC LibVlc { get; }

        private readonly Rect defaultSourceRect;
        private ChapterCue? chapter;
        private Rect normalizedSourceRect;
        private bool readyToPlay;
        private bool updateMediaProperties;
        private TimeSpan naturalDuration;
        private TimeSpan position;
        private MediaPlaybackState playbackState;
        private PlaybackItem? playbackItem;

        public VlcMediaPlayer(LibVLC libVlc)
        {
            LibVlc = libVlc;
            VlcPlayer = new MediaPlayer(libVlc);
            defaultSourceRect = new Rect(0, 0, 1, 1);
            normalizedSourceRect = defaultSourceRect;

            VlcPlayer.TimeChanged += VlcPlayer_TimeChanged;
            VlcPlayer.Muted += (s, e) => IsMutedChanged?.Invoke(this, EventArgs.Empty);
            VlcPlayer.Unmuted += (s, e) => IsMutedChanged?.Invoke(this, EventArgs.Empty);
            VlcPlayer.VolumeChanged += (s, e) => VolumeChanged?.Invoke(this, EventArgs.Empty);
            VlcPlayer.Paused += (s, e) => PlaybackState = MediaPlaybackState.Paused;
            VlcPlayer.Stopped += (s, e) => PlaybackState = MediaPlaybackState.None;
            VlcPlayer.EncounteredError += VlcPlayer_EncounteredError;
            VlcPlayer.Playing += VlcPlayer_Playing;
            VlcPlayer.ChapterChanged += VlcPlayer_ChapterChanged;
            VlcPlayer.LengthChanged += VlcPlayer_LengthChanged;
            VlcPlayer.EndReached += VlcPlayer_EndReached;
            VlcPlayer.Buffering += VlcPlayer_Buffering;
            VlcPlayer.Opening += VlcPlayer_Opening;
            VlcPlayer.ESAdded += VlcPlayer_ESAdded;
            VlcPlayer.ESSelected += VlcPlayer_ESSelected;
            VlcPlayer.SeekableChanged += VlcPlayer_SeekableChanged;

            // Notify VLC to auto detect new audio device on device changed
            MediaDevice.DefaultAudioRenderDeviceChanged += MediaDevice_DefaultAudioRenderDeviceChanged;
        }

        private void VlcPlayer_SeekableChanged(object sender, MediaPlayerSeekableChangedEventArgs e)
        {
            bool seekable = e.Seekable > 0;
            if (CanSeek == seekable) return;
            CanSeek = seekable;
            CanSeekChanged?.Invoke(this, EventArgs.Empty);
        }

        private void VlcPlayer_TimeChanged(object sender, MediaPlayerTimeChangedEventArgs e)
        {
            TimeSpan newValue = TimeSpan.FromMilliseconds(e.Time);
            if (newValue != position)
            {
                TimeSpan oldValue = position;
                position = newValue;
                PositionChanged?.Invoke(this, new ValueChangedEventArgs<TimeSpan>(newValue, oldValue));
            }
        }

        private void VlcPlayer_EncounteredError(object sender, EventArgs e)
        {
            MediaFailed?.Invoke(this, EventArgs.Empty);
            PlaybackState = MediaPlaybackState.None;
        }

        private void VlcPlayer_ESSelected(object sender, MediaPlayerESSelectedEventArgs e)
        {
            if (PlaybackItem == null || e.Type != TrackType.Text) return;
            // VLC sometimes auto selects a subtitle track.
            // Update subtitle track index accordingly.
            int spu = e.Id;
            if (spu < 0) return;
            PlaybackItem.SubtitleTracks.SelectVlcSpu(spu);
        }

        private void VlcPlayer_ChapterChanged(object sender, MediaPlayerChapterChangedEventArgs e)
        {
            if (PlaybackItem == null || e.Chapter < 0 || e.Chapter >= PlaybackItem.Chapters.Count)
            {
                Chapter = null;
                return;
            }

            Chapter = PlaybackItem.Chapters[e.Chapter];
        }

        private void VlcPlayer_ESAdded(object sender, MediaPlayerESAddedEventArgs e)
        {
            // Cannot use this event to check the track details during media Opening
            // This event fires before the track entry is added to Media.Tracks array
            if (PlaybackItem == null || PlaybackState == MediaPlaybackState.Opening) return;
            if (e.Type == TrackType.Text)
            {
                PlaybackItem.SubtitleTracks.NotifyTrackAdded(e.Id);
            }
        }

        private void VlcPlayer_LengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            NaturalDuration = TimeSpan.FromMilliseconds(e.Length);
        }

        private void VlcPlayer_Playing(object sender, EventArgs e)
        {
            PlaybackState = MediaPlaybackState.Playing;
            if (!updateMediaProperties) return;
            updateMediaProperties = false;

            // Update video dimension
            uint px = 0, py = 0;
            VlcPlayer.Size(0, ref px, ref py);
            if (NaturalVideoWidth != px || NaturalVideoHeight != py)
            {
                NaturalVideoWidth = px;
                NaturalVideoHeight = py;
                NaturalVideoSizeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void VlcPlayer_EndReached(object sender, EventArgs e)
        {
            MediaEnded?.Invoke(this, EventArgs.Empty);
            PlaybackState = MediaPlaybackState.None;
            if (IsLoopingEnabled)
                Replay();
        }

        private void VlcPlayer_Opening(object sender, EventArgs e)
        {
            MediaOpened?.Invoke(this, EventArgs.Empty);
            NaturalDuration = TimeSpan.Zero;
            PlaybackState = MediaPlaybackState.Opening;
        }

        private void VlcPlayer_Buffering(object sender, MediaPlayerBufferingEventArgs e)
        {
            if (BufferingProgress == 0)
            {
                BufferingStarted?.Invoke(this, EventArgs.Empty);
                //PlaybackStateChanged?.Invoke(this, null);
            }

            BufferingProgress = e.Cache / 100d;
            BufferingProgressChanged?.Invoke(this, EventArgs.Empty);
            if (BufferingProgress == 1.0)
            {
                BufferingEnded?.Invoke(this, EventArgs.Empty);
                //PlaybackStateChanged?.Invoke(this, null);
                BufferingProgress = 0;
            }
        }

        private void RemoveItemHandlers(PlaybackItem item)
        {
            item.SubtitleTracks.SelectedIndexChanged -= SubtitleTracksOnSelectedIndexChanged;
            item.AudioTracks.SelectedIndexChanged -= AudioTracksOnSelectedIndexChanged;
            item.VideoTracks.SelectedIndexChanged -= VideoTracksOnSelectedIndexChanged;
        }

        private void RegisterItemHandlers(PlaybackItem item)
        {
            RemoveItemHandlers(item);
            item.SubtitleTracks.SelectedIndexChanged += SubtitleTracksOnSelectedIndexChanged;
            item.AudioTracks.SelectedIndexChanged += AudioTracksOnSelectedIndexChanged;
            item.VideoTracks.SelectedIndexChanged += VideoTracksOnSelectedIndexChanged;
        }

        private void AudioTracksOnSelectedIndexChanged(ISingleSelectMediaTrackList sender, object? args)
        {
            PlaybackAudioTrackList trackList = (PlaybackAudioTrackList)sender;
            VlcPlayer.SetAudioTrack(sender.SelectedIndex < 0 ? -1 : trackList[sender.SelectedIndex].VlcTrackId);
        }

        private void VideoTracksOnSelectedIndexChanged(ISingleSelectMediaTrackList sender, object? args)
        {
            PlaybackVideoTrackList trackList = (PlaybackVideoTrackList)sender;
            VlcPlayer.SetVideoTrack(sender.SelectedIndex < 0 ? -1 : trackList[sender.SelectedIndex].VlcTrackId);
        }

        private void SubtitleTracksOnSelectedIndexChanged(ISingleSelectMediaTrackList sender, object? args)
        {
            PlaybackSubtitleTrackList trackList = (PlaybackSubtitleTrackList)sender;
            if (sender.SelectedIndex < 0) VlcPlayer.SetSpu(-1);
            else if (trackList[sender.SelectedIndex].VlcSpu >= 0)   // VlcSpu < 0 on lazy load
                VlcPlayer.SetSpu(trackList[sender.SelectedIndex].VlcSpu);
        }

        public void AddSubtitle(IStorageFile file, bool select = true)
        {
            if (PlaybackItem == null) return;
            string mrl = "winrt://" + StorageApplicationPermissions.FutureAccessList.Add(file, "subtitle");
            VlcPlayer.AddSlave(MediaSlaveType.Subtitle, mrl, select);
        }

        public void Close()
        {
            VlcPlayer.Dispose();
        }

        public void Pause()
        {
            if (PlaybackState != MediaPlaybackState.Playing) return;
            VlcPlayer.Pause();
        }

        public void Play()
        {
            if (PlaybackItem?.Media == null) return;
            if (readyToPlay)
            {
                readyToPlay = false;
                VlcPlayer.Play(PlaybackItem.Media);
            }
            else
            {
                if (VlcPlayer.State == VLCState.Ended)
                    VlcPlayer.Stop();

                VlcPlayer.Play();
            }
        }

        public void StepBackwardOneFrame()
        {
            Position -= TimeSpan.FromSeconds(.042);
        }

        public void StepForwardOneFrame()
        {
            VlcPlayer.NextFrame();
        }

        private void Replay()
        {
            VlcPlayer.Stop();
            VlcPlayer.Play();
        }

        private void MediaDevice_DefaultAudioRenderDeviceChanged(object sender, DefaultAudioRenderDeviceChangedEventArgs args)
        {
            if (args.Role == AudioDeviceRole.Default)
            {
                VlcPlayer.SetOutputDevice(args.Id);
            }
        }
    }
}
