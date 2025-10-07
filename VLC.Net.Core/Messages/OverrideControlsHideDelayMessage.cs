namespace VLC.Net.Core.Messages
{
    public sealed record OverrideControlsHideDelayMessage(int Delay)
    {
        public int Delay { get; } = Delay;
    }
}
