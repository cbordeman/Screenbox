using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using VLC.Net.Core.Models;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels;

// Copyright (c) Dani John
// Licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.
// Source: https://github.com/rocksdanister/lively
public sealed partial class LivelyWallpaperSelectorViewModel : ObservableRecipient,
    IRecipient<PropertyChangedMessage<LivelyWallpaperModel?>>
{
    readonly Dispatcher dispatcherQueue;
    
    public ObservableCollection<LivelyWallpaperModel> Visualizers { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private LivelyWallpaperModel? selectedVisualizer;

    private readonly LivelyWallpaperModel @default = new()
    {
        IsPreset = true,
        Path = string.Empty,
        Model = new LivelyInfoModel { Title = "Default" }
    };

    private readonly ILivelyWallpaperService wallpaperService;
    private readonly IFilesService filesService;
    private readonly ISettingsService settingsService;
    
    public LivelyWallpaperSelectorViewModel(ILivelyWallpaperService wallpaperService, IFilesService filesService, ISettingsService settingsService,
        string defaultTitle, string defaultPreviewPath) : this(wallpaperService, filesService, settingsService)
    {
        @default.PreviewPath = defaultPreviewPath;
        @default.Model.Title = defaultTitle;

        IsActive = true;
    }

    [DependencyInjectionConstructor]
    public LivelyWallpaperSelectorViewModel(ILivelyWallpaperService wallpaperService, IFilesService filesService, ISettingsService settingsService)
    {
        this.wallpaperService = wallpaperService;
        this.filesService = filesService;
        this.settingsService = settingsService;
        dispatcherQueue = Dispatcher.UIThread;
    }

    public async Task InitializeVisualizers()
    {
        if (Visualizers.Count > 0) return;
        await LoadVisualizers();
    }

    public async Task LoadVisualizers()
    {
        var availableVisualizers = await wallpaperService.GetAvailableVisualizersAsync();
        availableVisualizers.Insert(0, @default);
        Visualizers.SyncItems(availableVisualizers);

        SelectedVisualizer =
            Visualizers.FirstOrDefault(visualizer => string.Equals(visualizer.Path,
                settingsService.LivelyActivePath, StringComparison.OrdinalIgnoreCase)) ??
            Visualizers[0];
    }

    public void Receive(PropertyChangedMessage<LivelyWallpaperModel?> message)
    {
        dispatcherQueue.Post(() => SelectedVisualizer = message.NewValue);
    }

    partial void OnSelectedVisualizerChanged(LivelyWallpaperModel? value)
    {
        // Ignore null value. Null is only a temporary value
        if (value == null) return;
        settingsService.LivelyActivePath = value.Path;
    }

    [RelayCommand]
    private Task OpenWallpaperLocation(LivelyWallpaperModel model)
    {
        try
        {
            Launcher.OpenFileOrFolderInUi(model.Path);
        }
        catch
        {
            // Optional: Show error msg.
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task BrowseVisualizer()
    {
        var files = await filesService.PickMultipleFilesAsync(".zip");
        if (files is null || files.Count == 0)
            return;

        if (files.Count > 1)
        {
            foreach (var file in files)
                await InstallVisualizer(file);
        }
        else
        {
            // Optional: Ask for user confirmation before install.
            var model = await InstallVisualizer(files.First());
            if (model is not null)
                SelectedVisualizer = model;
        }
    }

    private async Task<LivelyWallpaperModel?> InstallVisualizer(IStorageFile wallpaperFile)
    {
        var wallpaperModel = await wallpaperService.InstallVisualizerAsync(wallpaperFile);
        if (wallpaperModel != null) Visualizers.Add(wallpaperModel);
        return wallpaperModel;
    }

    private bool CanDeleteVisualizer(LivelyWallpaperModel? visualizer) => visualizer is { IsPreset: false };

    [RelayCommand(CanExecute = nameof(CanDeleteVisualizer))]
    private Task DeleteVisualizer(LivelyWallpaperModel? visualizer)
    {
        if (visualizer is null || visualizer.IsPreset)
            return Task.CompletedTask;

        if (SelectedVisualizer == visualizer)
            SelectedVisualizer = Visualizers.FirstOrDefault();
        Visualizers.Remove(visualizer);

        try
        {
            var folder = Path.GetDirectoryName(visualizer.Path);
            if (folder is null)
                throw new Exception($"Couldn't get folder name from {visualizer.Path}.");
            Directory.Delete(folder);
        }
#pragma warning disable CS0168 // Variable is declared but never used
        catch (Exception ex)
#pragma warning restore CS0168 // Variable is declared but never used
        {
            // Show error.
        }
        return Task.CompletedTask;
    }
}