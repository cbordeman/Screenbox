#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using VLC.Net.Core.Enums;
using VLC.Net.Core.Helpers;
using VLC.Net.Core.Messages;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class SettingsPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private int playerAutoResize;
        [ObservableProperty] private bool playerVolumeGesture;
        [ObservableProperty] private bool playerSeekGesture;
        [ObservableProperty] private bool playerTapGesture;
        [ObservableProperty] private bool playerShowControls;
        [ObservableProperty] private bool playerShowChapters;
        [ObservableProperty] private int volumeBoost;
        [ObservableProperty] private bool useIndexer;
        [ObservableProperty] private bool showRecent;
        [ObservableProperty] private int theme;
        [ObservableProperty] private bool enqueueAllFilesInFolder;
        [ObservableProperty] private bool restorePlaybackPosition;
        [ObservableProperty] private bool searchRemovableStorage;
        [ObservableProperty] private bool advancedMode;
        [ObservableProperty] private int videoUpscaling;
        [ObservableProperty] private bool useMultipleInstances;
        [ObservableProperty] private string globalArguments;
        [ObservableProperty] private bool isRelaunchRequired;
        [ObservableProperty] private int selectedLanguage;

        public ObservableCollection<StorageFolder> MusicLocations { get; }

        public ObservableCollection<StorageFolder> VideoLocations { get; }

        public ObservableCollection<StorageFolder> RemovableStorageFolders { get; }

        public List<Models.Language> AvailableLanguages { get; }

        private readonly ISettingsService settingsService;
        private readonly ILibraryService libraryService;
        private readonly DispatcherQueue dispatcherQueue;
        private readonly DispatcherQueueTimer storageDeviceRefreshTimer;
        private readonly DeviceWatcher? portableStorageDeviceWatcher;
        private static InitialValues? initialValues;
        private StorageLibrary? videosLibrary;
        private StorageLibrary? musicLibrary;

        private record InitialValues(string GlobalArguments, bool AdvancedMode, int VideoUpscaling, int Language)
        {
            public string GlobalArguments { get; } = GlobalArguments;
            public bool AdvancedMode { get; } = AdvancedMode;
            public int VideoUpscaling { get; } = VideoUpscaling;
            public int Language { get; } = Language;
        }

        public SettingsPageViewModel(ISettingsService settingsService, ILibraryService libraryService)
        {
            this.settingsService = settingsService;
            this.libraryService = libraryService;
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            storageDeviceRefreshTimer = dispatcherQueue.CreateTimer();
            MusicLocations = new ObservableCollection<StorageFolder>();
            VideoLocations = new ObservableCollection<StorageFolder>();
            RemovableStorageFolders = new ObservableCollection<StorageFolder>();

            var manifestLanguages = ApplicationLanguages.ManifestLanguages.Select(l => new Language(l)).ToList();
            AvailableLanguages = manifestLanguages.Select(l => new Models.Language(l.NativeName, l.LanguageTag, l.LayoutDirection))
                .OrderBy(l => l.NativeName, StringComparer.CurrentCultureIgnoreCase)
                .Prepend(new Models.Language(string.Empty, string.Empty, LanguageLayoutDirection.Ltr))
                .ToList();

            if (SystemInformation.IsXbox)
            {
                portableStorageDeviceWatcher = DeviceInformation.CreateWatcher(DeviceClass.PortableStorageDevice);
                portableStorageDeviceWatcher.Updated += OnPortableStorageDeviceChanged;
                portableStorageDeviceWatcher.Removed += OnPortableStorageDeviceChanged;
                portableStorageDeviceWatcher.Start();
            }

            // Load values
            playerAutoResize = (int)this.settingsService.PlayerAutoResize;
            playerVolumeGesture = this.settingsService.PlayerVolumeGesture;
            playerSeekGesture = this.settingsService.PlayerSeekGesture;
            playerTapGesture = this.settingsService.PlayerTapGesture;
            playerShowControls = this.settingsService.PlayerShowControls;
            playerShowChapters = this.settingsService.PlayerShowChapters;
            useIndexer = this.settingsService.UseIndexer;
            showRecent = this.settingsService.ShowRecent;
            theme = ((int)this.settingsService.Theme + 2) % 3;
            enqueueAllFilesInFolder = this.settingsService.EnqueueAllFilesInFolder;
            restorePlaybackPosition = this.settingsService.RestorePlaybackPosition;
            searchRemovableStorage = this.settingsService.SearchRemovableStorage;
            advancedMode = this.settingsService.AdvancedMode;
            useMultipleInstances = this.settingsService.UseMultipleInstances;
            videoUpscaling = (int)this.settingsService.VideoUpscale;
            globalArguments = this.settingsService.GlobalArguments;
            int maxVolume = this.settingsService.MaxVolume;
            volumeBoost = maxVolume switch
            {
                >= 200 => 3,
                >= 150 => 2,
                >= 125 => 1,
                _ => 0
            };

            string currentLanguage = ApplicationLanguages.PrimaryLanguageOverride;
            selectedLanguage = AvailableLanguages.FindIndex(l => l.LanguageTag.Equals(currentLanguage));

            // Setting initial values for relaunch check
            initialValues ??= new InitialValues(globalArguments, advancedMode, videoUpscaling, selectedLanguage);
            CheckForRelaunch();

            IsActive = true;
        }

        partial void OnThemeChanged(int value)
        {
            // The recommended theme option order is Light, Dark, System
            // So we need to map the value to the correct ThemeOption
            settingsService.Theme = (ThemeOption)((value + 1) % 3);
            Messenger.Send(new SettingsChangedMessage(nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.Theme), typeof(SettingsPageViewModel)));
        }

        partial void OnSelectedLanguageChanged(int value)
        {
            if (value <= 0)
            {
                ApplicationLanguages.PrimaryLanguageOverride = string.Empty;
                CheckForRelaunch();
                return;
            }

            // If the value is out of bounds, do nothing
            if (value >= AvailableLanguages.Count) return;
            ApplicationLanguages.PrimaryLanguageOverride = AvailableLanguages[value].LanguageTag;
            CheckForRelaunch();
        }

        partial void OnPlayerAutoResizeChanged(int value)
        {
            settingsService.PlayerAutoResize = (PlayerAutoResizeOption)value;
            Messenger.Send(new SettingsChangedMessage(nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.PlayerAutoResize), typeof(SettingsPageViewModel)));
        }

        partial void OnPlayerVolumeGestureChanged(bool value)
        {
            settingsService.PlayerVolumeGesture = value;
            Messenger.Send(new SettingsChangedMessage(nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.PlayerVolumeGesture), typeof(SettingsPageViewModel)));
        }

        partial void OnPlayerSeekGestureChanged(bool value)
        {
            settingsService.PlayerSeekGesture = value;
            Messenger.Send(new SettingsChangedMessage(nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.PlayerSeekGesture), typeof(SettingsPageViewModel)));
        }

        partial void OnPlayerTapGestureChanged(bool value)
        {
            settingsService.PlayerTapGesture = value;
            Messenger.Send(new SettingsChangedMessage(nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.PlayerTapGesture), typeof(SettingsPageViewModel)));
        }

        partial void OnPlayerShowControlsChanged(bool value)
        {
            settingsService.PlayerShowControls = value;
            Messenger.Send(new SettingsChangedMessage(nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.PlayerShowControls), typeof(SettingsPageViewModel)));
        }

        partial void OnPlayerShowChaptersChanged(bool value)
        {
            settingsService.PlayerShowChapters = value;
            Messenger.Send(new SettingsChangedMessage(nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.PlayerShowChapters), typeof(SettingsPageViewModel)));
        }

        partial void OnUseIndexerChanged(bool value)
        {
            settingsService.UseIndexer = value;
            Messenger.Send(new SettingsChangedMessage(nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.UseIndexer), typeof(SettingsPageViewModel)));
        }

        partial void OnShowRecentChanged(bool value)
        {
            settingsService.ShowRecent = value;
            Messenger.Send(new SettingsChangedMessage(nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.ShowRecent), typeof(SettingsPageViewModel)));
        }

        partial void OnEnqueueAllFilesInFolderChanged(bool value)
        {
            settingsService.EnqueueAllFilesInFolder = value;
            Messenger.Send(new SettingsChangedMessage(nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.EnqueueAllFilesInFolder), typeof(SettingsPageViewModel)));
        }

        partial void OnRestorePlaybackPositionChanged(bool value)
        {
            settingsService.RestorePlaybackPosition = value;
            Messenger.Send(new SettingsChangedMessage(nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.RestorePlaybackPosition), typeof(SettingsPageViewModel)));
        }

        async partial void OnSearchRemovableStorageChanged(bool value)
        {
            settingsService.SearchRemovableStorage = value;
            Messenger.Send(new SettingsChangedMessage(nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.SearchRemovableStorage), typeof(SettingsPageViewModel)));

            if (SystemInformation.IsXbox && RemovableStorageFolders.Count > 0)
            {
                await RefreshLibrariesAsync();
            }
        }

        partial void OnVolumeBoostChanged(int value)
        {
            settingsService.MaxVolume = value switch
            {
                3 => 200,
                2 => 150,
                1 => 125,
                _ => 100
            };
            Messenger.Send(new SettingsChangedMessage(nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.VolumeBoost), typeof(SettingsPageViewModel)));
        }

        partial void OnAdvancedModeChanged(bool value)
        {
            settingsService.AdvancedMode = value;
            Messenger.Send(new SettingsChangedMessage(nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.AdvancedMode), typeof(SettingsPageViewModel)));
            CheckForRelaunch();
        }

        partial void OnVideoUpscalingChanged(int value)
        {
            settingsService.VideoUpscale = (VideoUpscaleOption)value;
            Messenger.Send(new SettingsChangedMessage(nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.VideoUpscaling), typeof(SettingsPageViewModel)));
            CheckForRelaunch();
        }

        partial void OnUseMultipleInstancesChanged(bool value)
        {
            settingsService.UseMultipleInstances = value;
            Messenger.Send(new SettingsChangedMessage(nameof(Screenbox.Core.ViewModels.SettingsPageViewModel.UseMultipleInstances), typeof(SettingsPageViewModel)));
        }

        partial void OnGlobalArgumentsChanged(string value)
        {
            // No need to broadcast SettingsChangedMessage for this option
            if (value != settingsService.GlobalArguments)
            {
                settingsService.GlobalArguments = value;
            }

            GlobalArguments = settingsService.GlobalArguments;
            CheckForRelaunch();
        }

        [RelayCommand]
        private async Task RefreshLibrariesAsync()
        {
            await Task.WhenAll(RefreshMusicLibrary(), RefreshVideosLibrary());
        }

        [RelayCommand]
        private async Task AddVideosFolderAsync()
        {
            if (videosLibrary == null) return;
            await videosLibrary.RequestAddFolderAsync();
        }

        [RelayCommand]
        private async Task RemoveVideosFolderAsync(StorageFolder folder)
        {
            if (videosLibrary == null) return;
            try
            {
                await videosLibrary.RequestRemoveFolderAsync(folder);
            }
            catch (Exception)
            {
                // System.Exception: The remote procedure call was cancelled.
                // pass
            }
        }

        [RelayCommand]
        private async Task AddMusicFolderAsync()
        {
            if (musicLibrary == null) return;
            await musicLibrary.RequestAddFolderAsync();
        }

        [RelayCommand]
        private async Task RemoveMusicFolderAsync(StorageFolder folder)
        {
            if (musicLibrary == null) return;
            try
            {
                await musicLibrary.RequestRemoveFolderAsync(folder);
            }
            catch (Exception)
            {
                // System.Exception: The remote procedure call was cancelled.
                // pass
            }
        }

        [RelayCommand]
        private void ClearRecentHistory()
        {
            StorageApplicationPermissions.MostRecentlyUsedList.Clear();
        }

        public void OnNavigatedFrom()
        {
            if (SystemInformation.IsXbox)
                portableStorageDeviceWatcher?.Stop();
        }

        public async Task LoadLibraryLocations()
        {
            if (videosLibrary == null)
            {
                if (libraryService.VideosLibrary == null)
                {
                    try
                    {
                        await libraryService.InitializeVideosLibraryAsync();
                    }
                    catch (Exception)
                    {
                        // pass
                    }
                }

                videosLibrary = libraryService.VideosLibrary;
                if (videosLibrary != null)
                {
                    videosLibrary.DefinitionChanged += LibraryOnDefinitionChanged;
                }
            }

            if (musicLibrary == null)
            {
                if (libraryService.MusicLibrary == null)
                {
                    try
                    {
                        await libraryService.InitializeMusicLibraryAsync();
                    }
                    catch (Exception)
                    {
                        // pass
                    }
                }

                musicLibrary = libraryService.MusicLibrary;
                if (musicLibrary != null)
                {
                    musicLibrary.DefinitionChanged += LibraryOnDefinitionChanged;
                }
            }

            UpdateLibraryLocations();
            await UpdateRemovableStorageFoldersAsync();
        }

        private void LibraryOnDefinitionChanged(StorageLibrary sender, object args)
        {
            dispatcherQueue.TryEnqueue(UpdateLibraryLocations);
        }

        private void OnPortableStorageDeviceChanged(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            async void RefreshAction() => await UpdateRemovableStorageFoldersAsync();
            storageDeviceRefreshTimer.Debounce(RefreshAction, TimeSpan.FromMilliseconds(500));
        }

        private void UpdateLibraryLocations()
        {
            if (videosLibrary != null)
            {
                VideoLocations.Clear();
                foreach (StorageFolder folder in videosLibrary.Folders)
                {
                    VideoLocations.Add(folder);
                }
            }

            if (musicLibrary != null)
            {
                MusicLocations.Clear();

                foreach (StorageFolder folder in musicLibrary.Folders)
                {
                    MusicLocations.Add(folder);
                }
            }
        }

        private async Task UpdateRemovableStorageFoldersAsync()
        {
            if (SystemInformation.IsXbox)
            {
                RemovableStorageFolders.Clear();
                var accessStatus = await KnownFolders.RequestAccessAsync(KnownFolderId.RemovableDevices);
                if (accessStatus != KnownFoldersAccessStatus.Allowed)
                    return;

                foreach (StorageFolder folder in await KnownFolders.RemovableDevices.GetFoldersAsync())
                {
                    RemovableStorageFolders.Add(folder);
                }
            }
        }

        private async Task RefreshMusicLibrary()
        {
            try
            {
                await libraryService.FetchMusicAsync(false);
            }
            catch (UnauthorizedAccessException)
            {
                Messenger.Send(new RaiseLibraryAccessDeniedNotificationMessage(KnownLibraryId.Music));
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(null, e.Message));
                LogService.Log(e);
            }
        }

        private async Task RefreshVideosLibrary()
        {
            try
            {
                await libraryService.FetchVideosAsync(false);
            }
            catch (UnauthorizedAccessException)
            {
                Messenger.Send(new RaiseLibraryAccessDeniedNotificationMessage(KnownLibraryId.Videos));
            }
            catch (Exception e)
            {
                Messenger.Send(new ErrorMessage(null, e.Message));
                LogService.Log(e);
            }
        }

        private void CheckForRelaunch()
        {
            if (initialValues == null) return;

            // Check if upscaling mode has been changed
            bool upscalingChanged = initialValues.VideoUpscaling != VideoUpscaling;

            // Check if app language has been changed
            bool languageChanged = initialValues.Language != SelectedLanguage;

            // Check if global arguments have been changed
            bool argsChanged = initialValues.GlobalArguments != settingsService.GlobalArguments;

            // Check if advanced mode has been changed
            bool modeChanged = initialValues.AdvancedMode != AdvancedMode;

            // Check if there are any global arguments set
            bool hasArgs = settingsService.GlobalArguments.Length > 0;

            // Check if advanced mode is on, and if global arguments are set
            bool whenOn = modeChanged && AdvancedMode && hasArgs;

            // Check if advanced mode is off, and if global arguments are set or have been removed
            bool whenOff = modeChanged && !AdvancedMode && (!hasArgs && argsChanged || hasArgs);

            // Require relaunch when advanced mode is on and global arguments have been changed
            bool whenOnAndChanged = AdvancedMode && argsChanged;

            // Combine everything
            IsRelaunchRequired = upscalingChanged || languageChanged || whenOn || whenOff || whenOnAndChanged;
        }
    }
}
