using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VLC.Net.Core.Enums;
using VLC.Net.Database;

namespace VLC.Net.Core.Services
{
    public sealed class SettingsService : ISettingsService
    {
        readonly SettingsDbContext dbContext;
        readonly Dictionary<string, object?> cache = new();
        
        public SettingsService(IDbContextFactory<SettingsDbContext> dbContextFactory)
        {
            this.dbContext = dbContextFactory.CreateDbContext();
        
            // Load values from the database.
            var settings = dbContext.Settings.ToList();
            foreach (var setting in settings)
                cache.Add(setting.Key, 
                    setting.Value == null ? null : 
                    JsonSerializer.Deserialize<object>(setting.Value));
            
            SetDefault(PlayerAutoResizeKey, (int)PlayerAutoResizeOption.Never);
            SetDefault(PlayerVolumeGestureKey, true);
            SetDefault(PlayerSeekGestureKey, true);
            SetDefault(PlayerTapGestureKey, true);
            SetDefault(PlayerShowControlsKey, true);
            SetDefault(PersistentVolumeKey, 100);
            SetDefault(MaxVolumeKey, 100);
            SetDefault(LibrariesUseIndexerKey, true);
            SetDefault(LibrariesSearchRemovableStorageKey, true);
            SetDefault(GeneralShowRecent, true);
            SetDefault(PersistentRepeatModeKey, (int)MediaPlaybackAutoRepeatMode.None);
            SetDefault(AdvancedModeKey, false);
            SetDefault(AdvancedVideoUpscaleKey, (int)VideoUpscaleOption.Linear);
            SetDefault(AdvancedMultipleInstancesKey, false);
            SetDefault(GlobalArgumentsKey, string.Empty);
            SetDefault(PlayerShowChaptersKey, true);

            // Device-family-specific overrides
            if (SystemInformation.IsXbox)
            {
                SetValue(PlayerTapGestureKey, false);
                SetValue(PlayerSeekGestureKey, false);
                SetValue(PlayerVolumeGestureKey, false);
                SetValue(PlayerAutoResizeKey, (int)PlayerAutoResizeOption.Never);
                SetValue(PlayerShowControlsKey, true);
            }
            
            dbContext.SaveChanges();
        }

        const string GeneralThemeKey = "General/Theme";
        const string PlayerAutoResizeKey = "Player/AutoResize";
        const string PlayerVolumeGestureKey = "Player/Gesture/Volume";
        const string PlayerSeekGestureKey = "Player/Gesture/Seek";
        const string PlayerTapGestureKey = "Player/Gesture/Tap";
        const string PlayerShowControlsKey = "Player/ShowControls";
        const string PlayerLivelyPathKey = "Player/Lively/Path";
        const string LibrariesUseIndexerKey = "Libraries/UseIndexer";
        const string LibrariesSearchRemovableStorageKey = "Libraries/SearchRemovableStorage";
        const string GeneralShowRecent = "General/ShowRecent";
        const string GeneralEnqueueAllInFolder = "General/EnqueueAllInFolder";
        const string GeneralRestorePlaybackPosition = "General/RestorePlaybackPosition";
        const string AdvancedModeKey = "Advanced/IsEnabled";
        const string AdvancedVideoUpscaleKey = "Advanced/VideoUpscale";
        const string AdvancedMultipleInstancesKey = "Advanced/MultipleInstances";
        const string GlobalArgumentsKey = "Values/GlobalArguments";
        const string PersistentVolumeKey = "Values/Volume";
        const string MaxVolumeKey = "Values/MaxVolume";
        const string PersistentRepeatModeKey = "Values/RepeatMode";
        const string PersistentSubtitleLanguageKey = "Values/SubtitleLanguage";
        const string PlayerShowChaptersKey = "Player/ShowChapters";

        public bool UseIndexer
        {
            get => GetValue<bool>(LibrariesUseIndexerKey);
            set => SetValue(LibrariesUseIndexerKey, value);
        }

        public ThemeOption Theme
        {
            get => (ThemeOption)GetValue<int>(GeneralThemeKey);
            set => SetValue(GeneralThemeKey, (int)value);
        }

        public PlayerAutoResizeOption PlayerAutoResize
        {
            get => (PlayerAutoResizeOption)GetValue<int>(PlayerAutoResizeKey);
            set => SetValue(PlayerAutoResizeKey, (int)value);
        }

        public bool PlayerVolumeGesture
        {
            get => GetValue<bool>(PlayerVolumeGestureKey);
            set => SetValue(PlayerVolumeGestureKey, value);
        }

        public bool PlayerSeekGesture
        {
            get => GetValue<bool>(PlayerSeekGestureKey);
            set => SetValue(PlayerSeekGestureKey, value);
        }

        public bool PlayerTapGesture
        {
            get => GetValue<bool>(PlayerTapGestureKey);
            set => SetValue(PlayerTapGestureKey, value);
        }

        public int PersistentVolume
        {
            get => GetValue<int>(PersistentVolumeKey);
            set => SetValue(PersistentVolumeKey, value);
        }

        public string PersistentSubtitleLanguage
        {
            get => GetRefValue<string>(PersistentSubtitleLanguageKey) ?? string.Empty;
            set => SetValue(PersistentSubtitleLanguageKey, value);
        }

        public int MaxVolume
        {
            get => GetValue<int>(MaxVolumeKey);
            set => SetValue(MaxVolumeKey, value);
        }

        public bool ShowRecent
        {
            get => GetValue<bool>(GeneralShowRecent);
            set => SetValue(GeneralShowRecent, value);
        }

        public bool EnqueueAllFilesInFolder
        {
            get => GetValue<bool>(GeneralEnqueueAllInFolder);
            set => SetValue(GeneralEnqueueAllInFolder, value);
        }

        public bool RestorePlaybackPosition
        {
            get => GetValue<bool>(GeneralRestorePlaybackPosition);
            set => SetValue(GeneralRestorePlaybackPosition, value);
        }

        public bool PlayerShowControls
        {
            get => GetValue<bool>(PlayerShowControlsKey);
            set => SetValue(PlayerShowControlsKey, value);
        }

        public bool SearchRemovableStorage
        {
            get => GetValue<bool>(LibrariesSearchRemovableStorageKey);
            set => SetValue(LibrariesSearchRemovableStorageKey, value);
        }

        public MediaPlaybackAutoRepeatMode PersistentRepeatMode
        {
            get => (MediaPlaybackAutoRepeatMode)GetValue<int>(PersistentRepeatModeKey);
            set => SetValue(PersistentRepeatModeKey, (int)value);
        }

        public string GlobalArguments
        {
            get => GetRefValue<string>(GlobalArgumentsKey) ?? string.Empty;
            set => SetValue(GlobalArgumentsKey, SanitizeArguments(value));
        }

        public bool AdvancedMode
        {
            get => GetValue<bool>(AdvancedModeKey);
            set => SetValue(AdvancedModeKey, value);
        }

        public VideoUpscaleOption VideoUpscale
        {
            get => (VideoUpscaleOption)GetValue<int>(AdvancedVideoUpscaleKey);
            set => SetValue(AdvancedVideoUpscaleKey, (int)value);
        }

        public bool UseMultipleInstances
        {
            get => GetValue<bool>(AdvancedMultipleInstancesKey);
            set => SetValue(AdvancedMultipleInstancesKey, value);
        }

        public string LivelyActivePath
        {
            get => GetRefValue<string>(PlayerLivelyPathKey) ?? string.Empty;
            set => SetValue(PlayerLivelyPathKey, value);
        }

        public bool PlayerShowChapters
        {
            get => GetValue<bool>(PlayerShowChaptersKey);
            set => SetValue(PlayerShowChaptersKey, value);
        }

        T GetValue<T>(string key) where T: struct
        {
            if (cache.TryGetValue(key, out object? value))
            {
                if (value is null)
                    throw new InvalidOperationException($"Value for key {key} is null.");
                return (T)value;
            }

            return default;
        }

        T? GetRefValue<T>(string key) where T: class
        {
            if (cache.TryGetValue(key, out object? value))
                return (T?)value;

            return null;
        }

        void SetValue<T>(string key, T? val)
        {
            if (typeof(T).IsValueType && val == null)
                throw new ArgumentNullException(nameof(val));
            cache[key] = val;
            var setting = CreateSetting(key, val);
            dbContext.Settings.Update(setting);
            dbContext.SaveChanges();
        }

        // This should only be called during the constructor to set defaults,
        // just after all the database values are loaded into cache.
        void SetDefault<T>(string key, T val)
        {
            if (typeof(T).IsValueType && val == null)
                throw new ArgumentNullException(nameof(val));
            
            if (cache.TryGetValue(key, out object? v))
            {
                // Already in db / cache, update value if different.
                if (!EqualityComparer<T>.Default.Equals(val, (T?)v))
                {
                    var setting = CreateSetting(key, val);
                    dbContext.Settings.Update(setting);
                    cache[key] = val;
                }
            }
            else
            {
                // Not in cache, must not be in db also, since all
                // the db settings were loaded into the cache before
                // any calls to this method.
                var setting = CreateSetting(key, val);
                dbContext.Settings.Add(setting);
                cache[key] = val;
            }
        }

        static Setting CreateSetting<T>(string key, T? val) =>
            new()
            {
                Key = key,
                Value = val == null ? null : JsonSerializer.Serialize(val)
            };

        static string SanitizeArguments(string raw)
        {
            string[] args = raw.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.StartsWith('-') && s != "--").ToArray();
            return string.Join(' ', args);
        }
    }
}