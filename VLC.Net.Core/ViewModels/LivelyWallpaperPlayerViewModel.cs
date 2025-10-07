#nullable enable

using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using VLC.Net.Core.Messages;
using VLC.Net.Core.Models;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels;

public partial class LivelyWallpaperPlayerViewModel : ObservableRecipient,
    IRecipient<PropertyChangedMessage<LivelyWallpaperModel?>>,
    IRecipient<PlaylistCurrentItemChangedMessage>
{
    public event EventHandler? TrackUpdateRequested;

    [ObservableProperty] private LivelyWallpaperModel? source;
    [ObservableProperty] private bool isLoading;

    private MediaViewModel? Media
    {
        get => media;
        set
        {
            if (media == value) return;
            var oldValue = media;
            media = value;
            if (oldValue != null)
                oldValue.PropertyChanged -= MediaOnPropertyChanged;

            if (value != null)
                value.PropertyChanged += MediaOnPropertyChanged;
        }
    }

    private MediaViewModel? media;

    private readonly ILivelyWallpaperService livelyService;
    private readonly ISettingsService settingsService;
    private readonly DispatcherQueueTimer timer;

    public LivelyWallpaperPlayerViewModel(ILivelyWallpaperService livelyService, ISettingsService settingsService)
    {
        this.livelyService = livelyService;
        this.settingsService = settingsService;
        timer = DispatcherQueue.GetForCurrentThread().CreateTimer();

        IsActive = true;
    }

    public void Receive(PropertyChangedMessage<LivelyWallpaperModel?> message)
    {
        Source = message.NewValue;
    }

    public void Receive(PlaylistCurrentItemChangedMessage message)
    {
        Media = message.Value;
        TrackUpdateRequested?.Invoke(this, EventArgs.Empty);
    }

    private void MediaOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Screenbox.Core.ViewModels.MediaViewModel.Name) or nameof(MediaViewModel.MainArtist))
        {
            timer.Debounce(() => TrackUpdateRequested?.Invoke(this, EventArgs.Empty), TimeSpan.FromMilliseconds(50));
        }
    }

    public void SendError(string title, string message)
    {
        Messenger.Send(new ErrorMessage(title, message));
    }

    public async Task LoadAsync()
    {
        var allVisualizers = await livelyService.GetAvailableVisualizersAsync();
        var activeVisualizerPath = settingsService.LivelyActivePath;

        var selectedVisualizer =
            allVisualizers.FirstOrDefault(visualizer =>
                visualizer.Path.Equals(activeVisualizerPath, StringComparison.OrdinalIgnoreCase));

        Source = selectedVisualizer;
        LoadMedia();
    }

    public async Task NavigatePage(WebView2 webView)
    {
        if (Source is null || webView.CoreWebView2 == null)
            return;

        if (string.IsNullOrEmpty(Source.Path) || string.IsNullOrEmpty(Source.Model.FileName))
        {
            webView.CoreWebView2.Navigate("about:blank");
        }
        else
        {
            var htmlPath = Path.Combine(Source.Path, Source.Model.FileName);
            webView.NavigateToLocalPath(htmlPath);
            await UpdateCurrentTrack(webView);
        }
    }

    // Ref: https://github.com/rocksdanister/lively/wiki/Web-Guide-IV-:-Interaction#controls
    public async Task UpdateLivelyProperties(WebView2 webView)
    {
        if (Source is null)
            return;

        var functionName = "livelyPropertyListener";
        var propertyPath = Path.Combine(Source.Path, "LivelyProperties.json");

        string jsonString;
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(propertyPath);
            jsonString = await FileIO.ReadTextAsync(file);
        }
        catch (Exception)
        {
            return;
        }

        var jsonObject = JObject.Parse(jsonString);
        foreach (KeyValuePair<string, JToken?> item in jsonObject)
        {
            var typeToken = item.Value?["type"];
            var valueToken = item.Value?["value"];
            if (typeToken == null || valueToken == null) continue;
            switch (typeToken.ToString())
            {
                case "slider":
                    await webView.ExecuteScriptFunctionAsync(functionName, item.Key, (double)valueToken);
                    break;
                case "dropdown":
                    await webView.ExecuteScriptFunctionAsync(functionName, item.Key, (int)valueToken);
                    break;
                case "checkbox":
                    await webView.ExecuteScriptFunctionAsync(functionName, item.Key, (bool)valueToken);
                    break;
                case "color":
                    await webView.ExecuteScriptFunctionAsync(functionName, item.Key, valueToken.ToString());
                    break;
                case "folderDropdown":
                    var relativePath = Path.Combine(item.Value?["folder"]?.ToString() ?? string.Empty,
                        valueToken.ToString());
                    var filePath = Path.Combine(Source.Path, relativePath);
                    await webView.ExecuteScriptFunctionAsync(functionName, item.Key,
                        File.Exists(filePath) ? relativePath : null);
                    break;
                case "button":
                case "label":
                    // Ignore, user action only.
                    break;
            }
        }
    }

    // Ref: https://github.com/rocksdanister/lively/wiki/Web-Guide-V-:-System-Data#--pause-event
    public async Task UpdatePauseState(WebView2 webView, bool isPaused)
    {
        if (Source is null || !Source.IsPauseNotify)
            return;

        var obj = new LivelyPlaybackStateModel()
        {
            IsPaused = isPaused
        };
        await webView.ExecuteScriptFunctionAsync("livelyWallpaperPlaybackChanged", obj);
    }

    // Ref: https://github.com/rocksdanister/lively/wiki/Web-Guide-V-:-System-Data#--system-nowplaying
    public async Task UpdateCurrentTrack(WebView2 webView)
    {
        if (Source is null || Media == null || webView.CoreWebView2 == null ||
            webView.CoreWebView2.IsSuspended || !Source.IsMusic)
            return;

        var model = new LivelyMusicModel
        {
            Title = Media.Name,
            Artist = Media.MainArtist?.Name,
            // Optional: Complete the mapping.
        };

        if (Media.Thumbnail != null)
        {
            using var thumbnailSource = await Media.GetThumbnailSourceAsync();
            var base64 = thumbnailSource != null ? await ReadToBase64Async(thumbnailSource) : string.Empty;
            model.Thumbnail = base64;
        }

        await webView.ExecuteScriptFunctionAsync("livelyCurrentTrack", JsonConvert.SerializeObject(model));
    }

    private void LoadMedia()
    {
        PlaylistInfo reply = Messenger.Send(new PlaylistRequestMessage());
        Media = reply.ActiveItem;
    }

    private static async Task<string> ReadToBase64Async(IRandomAccessStream source)
    {
        using var stream = source.CloneStream();

        var buffer = new byte[stream.Size];
        await stream.ReadAsync(buffer.AsBuffer(), (uint)stream.Size, InputStreamOptions.None);
        return Convert.ToBase64String(buffer);
    }
}
