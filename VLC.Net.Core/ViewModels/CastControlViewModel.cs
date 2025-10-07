#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VLC.Net.Core.Events;
using VLC.Net.Core.Models;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class CastControlViewModel : ObservableObject
    {
        public ObservableCollection<Renderer> Renderers { get; }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(Screenbox.Core.ViewModels.CastControlViewModel.CastCommand))]
        private Renderer? selectedRenderer;

        [ObservableProperty] private Renderer? castingDevice;
        [ObservableProperty] private bool isCasting;

        private readonly ICastService castService;
        private readonly DispatcherQueue dispatcherQueue;

        public CastControlViewModel(ICastService castService)
        {
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            this.castService = castService;
            this.castService.RendererFound += CastServiceOnRendererFound;
            this.castService.RendererLost += CastServiceOnRendererLost;
            Renderers = new ObservableCollection<Renderer>();
        }

        public void StartDiscovering()
        {
            if (IsCasting) return;
            castService.Start();
        }

        public void StopDiscovering()
        {
            castService.Stop();
            SelectedRenderer = null;
            Renderers.Clear();
        }

        [RelayCommand(CanExecute = nameof(CanCast))]
        private void Cast()
        {
            if (SelectedRenderer == null) return;
            SentrySdk.AddBreadcrumb("Start casting", category: "command", type: "user", data: new Dictionary<string, string>
            {
                {"rendererHash", SelectedRenderer.Name.GetHashCode().ToString()},
                {"rendererType", SelectedRenderer.Type},
                {"canRenderAudio", SelectedRenderer.CanRenderAudio.ToString()},
                {"canRenderVideo", SelectedRenderer.CanRenderVideo.ToString()},
            });
            castService.SetActiveRenderer(SelectedRenderer);
            CastingDevice = SelectedRenderer;
            IsCasting = true;
        }

        private bool CanCast() => SelectedRenderer is { IsAvailable: true };

        [RelayCommand]
        private void StopCasting()
        {
            SentrySdk.AddBreadcrumb("Stop casting", category: "command", type: "user");
            castService.SetActiveRenderer(null);
            IsCasting = false;
            StartDiscovering();
        }

        private void CastServiceOnRendererLost(object sender, RendererLostEventArgs e)
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                Renderers.Remove(e.Renderer);
                if (SelectedRenderer == e.Renderer) SelectedRenderer = null;
            });
        }

        private void CastServiceOnRendererFound(object sender, RendererFoundEventArgs e)
        {
            dispatcherQueue.TryEnqueue(() => Renderers.Add(e.Renderer));
        }
    }
}
