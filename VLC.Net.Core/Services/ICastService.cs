#nullable enable

using VLC.Net.Core.Events;
using VLC.Net.Core.Models;

namespace VLC.Net.Core.Services
{
    public interface ICastService
    {
        event EventHandler<RendererFoundEventArgs>? RendererFound;
        event EventHandler<RendererLostEventArgs>? RendererLost;
        bool SetActiveRenderer(Renderer? renderer);
        bool Start();
        void Stop();
    }
}