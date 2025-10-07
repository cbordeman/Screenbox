using VLC.Net.Core.Models;

namespace VLC.Net.Core.Events
{
    public sealed class RendererFoundEventArgs : EventArgs
    {
        public Renderer Renderer { get; }

        public RendererFoundEventArgs(Renderer renderer)
        {
            Renderer = renderer;
        }
    }

    public sealed class RendererLostEventArgs : EventArgs
    {
        public Renderer Renderer { get; }

        public RendererLostEventArgs(Renderer renderer)
        {
            Renderer = renderer;
        }
    }
}
