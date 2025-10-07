#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using VLC.Net.Core.Common;
using VLC.Net.Core.Factories;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class StorageItemViewModel : ObservableObject
    {
        public string Name { get; }

        public string Path { get; }

        public DateTimeOffset DateCreated { get; }

        public IStorageItem StorageItem { get; }

        public MediaViewModel? Media { get; }

        public bool IsFile { get; }

        [ObservableProperty] private string captionText;
        [ObservableProperty] private uint itemCount;

        private readonly IFilesService filesService;

        public StorageItemViewModel(IFilesService filesService,
            MediaViewModelFactory mediaFactory,
            IStorageItem storageItem)
        {
            this.filesService = filesService;
            StorageItem = storageItem;
            captionText = string.Empty;
            DateCreated = storageItem.DateCreated;

            if (storageItem is StorageFile file)
            {
                IsFile = true;
                Media = mediaFactory.GetSingleton(file);
                Name = Media.Name;
                Path = Media.Location;
            }
            else
            {
                Name = storageItem.Name;
                Path = storageItem.Path;
            }
        }

        public async Task UpdateCaptionAsync()
        {
            try
            {
                switch (StorageItem)
                {
                    case StorageFolder folder when !string.IsNullOrEmpty(folder.Path):
                        ItemCount = await filesService.GetSupportedItemCountAsync(folder);
                        break;
                    case StorageFile file:
                        if (!string.IsNullOrEmpty(Media?.Caption))
                        {
                            CaptionText = Media?.Caption ?? string.Empty;
                        }
                        else
                        {
                            string[] additionalPropertyKeys =
                            {
                                SystemProperties.Music.Artist,
                                SystemProperties.Media.Duration
                            };

                            IDictionary<string, object> additionalProperties =
                                await file.Properties.RetrievePropertiesAsync(additionalPropertyKeys);

                            if (additionalProperties[SystemProperties.Music.Artist] is string[] { Length: > 0 } contributingArtists)
                            {
                                CaptionText = string.Join(", ", contributingArtists);
                            }
                            else if (additionalProperties[SystemProperties.Media.Duration] is ulong ticks and > 0)
                            {
                                TimeSpan duration = TimeSpan.FromTicks((long)ticks);
                                CaptionText = Humanizer.ToDuration(duration);
                            }
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                LogService.Log(e);
            }
        }
    }
}
