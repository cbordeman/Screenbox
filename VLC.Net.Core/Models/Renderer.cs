#nullable enable

using LibVLCSharp.Shared;

namespace VLC.Net.Core.Models
{
    public sealed class Renderer
    {
        public bool IsAvailable { get; private set; }

        public string Name { get; }

        public string Type { get; }

        public string? IconUri { get; }

        public bool CanRenderVideo { get; }

        public bool CanRenderAudio { get; }

        internal RendererItem? Target => IsAvailable ? item : null;

        private readonly RendererItem item;

        internal Renderer(RendererItem item)
        {
            this.item = item;
            Name = item.Name;
            Type = item.Type;
            IconUri = item.IconUri;
            CanRenderVideo = item.CanRenderVideo;
            CanRenderAudio = item.CanRenderAudio;
            IsAvailable = true;
        }

        internal void Dispose()
        {
            IsAvailable = false;
            item.Dispose();
        }
    }
}
