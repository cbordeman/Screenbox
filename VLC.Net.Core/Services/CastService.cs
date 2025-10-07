#nullable enable

using LibVLCSharp.Shared;
using VLC.Net.Core.Events;
using VLC.Net.Core.Models;

namespace VLC.Net.Core.Services
{
    public sealed class CastService : ICastService
    {
        public event EventHandler<RendererFoundEventArgs>? RendererFound;
        public event EventHandler<RendererLostEventArgs>? RendererLost;

        private readonly LibVlcService libVlcService;
        private readonly List<Renderer> renderers;
        private RendererDiscoverer? discoverer;

        public CastService(LibVlcService libVlcService)
        {
            this.libVlcService = libVlcService;
            renderers = new List<Renderer>();
        }

        public bool SetActiveRenderer(Renderer? renderer)
        {
            return libVlcService.MediaPlayer?.VlcPlayer.SetRenderer(renderer?.Target) ?? false;
        }

        public bool Start()
        {
            Stop();
            LibVLC? libVlc = libVlcService.LibVlc;
            Guard.IsNotNull(libVlc, nameof(libVlc));
            discoverer = new RendererDiscoverer(libVlc);
            discoverer.ItemAdded += DiscovererOnItemAdded;
            discoverer.ItemDeleted += DiscovererOnItemDeleted;
            return discoverer.Start();
        }

        public void Stop()
        {
            if (discoverer == null) return;
            discoverer.Stop();
            discoverer.ItemAdded -= DiscovererOnItemAdded;
            discoverer.ItemDeleted -= DiscovererOnItemDeleted;
            discoverer.Dispose();
            discoverer = null;
            foreach (Renderer renderer in renderers)
            {
                renderer.Dispose();
            }

            renderers.Clear();
        }

        private void DiscovererOnItemAdded(object sender, RendererDiscovererItemAddedEventArgs e)
        {
            Guard.IsNotNull(discoverer, nameof(discoverer));
            Renderer renderer = new(e.RendererItem);
            renderers.Add(renderer);
            RendererFound?.Invoke(this, new RendererFoundEventArgs(renderer));
        }

        private void DiscovererOnItemDeleted(object sender, RendererDiscovererItemDeletedEventArgs e)
        {
            Renderer? item = renderers.Find(r => r.Target == e.RendererItem);
            if (item != null)
            {
                RendererLost?.Invoke(this, new RendererLostEventArgs(item));
            }
        }
    }
}
