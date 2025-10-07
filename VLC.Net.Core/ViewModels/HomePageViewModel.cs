using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using VLC.Net.Core.Factories;
using VLC.Net.Core.Messages;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class HomePageViewModel : ObservableRecipient,
        IRecipient<PlaylistCurrentItemChangedMessage>
    {
        private MostRecentlyUsedService mruService;

        public ObservableCollection<MediaViewModel> Recent { get; }

        public bool HasRecentMedia => mruService.Entries.Count > 0 && settingsService.ShowRecent;

        private readonly MediaViewModelFactory mediaFactory;
        private readonly IFilesService filesService;
        private readonly ISettingsService settingsService;
        private readonly Dictionary<string, string> pathToMruMappings;

        public HomePageViewModel(
            MediaViewModelFactory mediaFactory,
            IFilesService filesService,
            ISettingsService settingsService,
            MostRecentlyUsedService mruService)
        {
            this.mediaFactory = mediaFactory;
            this.filesService = filesService;
            this.settingsService = settingsService;
            this.mruService = mruService;
            pathToMruMappings = new Dictionary<string, string>();
            Recent = new ObservableCollection<MediaViewModel>();

            // Activate the view model's messenger
            IsActive = true;
        }

        public async void Receive(PlaylistCurrentItemChangedMessage message)
        {
            if (settingsService.ShowRecent)
            {
                await UpdateRecentMediaListAsync(false).ConfigureAwait(false);
            }
        }

        public async void OnLoaded()
        {
            await UpdateContentAsync();
        }

        [RelayCommand]
        private void OpenUrl(Uri? url)
        {
            if (url == null) return;
            Messenger.Send(new PlayMediaMessage(url));
        }

        private async Task UpdateContentAsync()
        {
            // Update recent media
            if (settingsService.ShowRecent)
            {
                await UpdateRecentMediaListAsync(true);
            }
            else
            {
                Recent.Clear();
            }
        }

        private async Task UpdateRecentMediaListAsync(bool loadMediaDetails)
        {
            string[] tokens = StorageApplicationPermissions.MostRecentlyUsedList.Entries
                .OrderByDescending(x => x.Metadata)
                .Select(x => x.Token)
                .Where(t => !string.IsNullOrEmpty(t))
                .ToArray();

            if (tokens.Length == 0)
            {
                Recent.Clear();
                return;
            }

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                StorageFile? file = await ConvertMruTokenToStorageFileAsync(token);
                if (file == null)
                {
                    try
                    {
                        StorageApplicationPermissions.MostRecentlyUsedList.Remove(token);
                    }
                    catch (Exception e)
                    {
                        LogService.Log(e);
                    }
                    continue;
                }

                // TODO: Add support for playing playlist file from home page
                if (file.IsSupportedPlaylist()) continue;
                if (!_dispatcher.HasThreadAccess)
                    throw new InvalidOperationException("This method must be called on the UI thread.");

                if (i >= Recent.Count)
                {
                    MediaViewModel media = mediaFactory.GetSingleton(file);
                    pathToMruMappings[media.Location] = token;
                    Recent.Add(media);
                }
                else if (Recent[i].Source is StorageFile existing)
                {
                    try
                    {
                        if (!file.IsEqual(existing)) MoveOrInsert(file, token, i);
                    }
                    catch (Exception)
                    {
                        // StorageFile.IsEqual() throws an exception
                        // System.Exception: Element not found. (Exception from HRESULT: 0x80070490)
                        // pass
                    }
                }
            }

            // Remove stale items
            while (Recent.Count > tokens.Length)
            {
                Recent.RemoveAt(Recent.Count - 1);
            }

            // Load media details for the remaining items
            if (!loadMediaDetails) return;
            IEnumerable<Task> loadingTasks = Recent.Select(x => x.LoadDetailsAsync(filesService));
            loadingTasks = Recent.Select(x => x.LoadThumbnailAsync()).Concat(loadingTasks);
            await Task.WhenAll(loadingTasks);
        }

        private void MoveOrInsert(StorageFile file, string token, int desiredIndex)
        {
            // Find index of the VM of the same file
            // There is no FindIndex method for ObservableCollection :(
            int existingIndex = -1;
            for (int j = desiredIndex + 1; j < Recent.Count; j++)
            {
                if (Recent[j].Source is StorageFile existingFile && file.IsEqual(existingFile))
                {
                    existingIndex = j;
                    break;
                }
            }

            if (existingIndex == -1)
            {
                MediaViewModel media = mediaFactory.GetSingleton(file);
                pathToMruMappings[media.Location] = token;
                Recent.Insert(desiredIndex, media);
            }
            else
            {
                MediaViewModel toInsert = Recent[existingIndex];
                Recent.RemoveAt(existingIndex);
                Recent.Insert(desiredIndex, toInsert);
            }
        }

        [RelayCommand]
        private void Play(MediaViewModel media)
        {
            if (media.IsMediaActive)
            {
                Messenger.Send(new TogglePlayPauseMessage(false));
            }
            else
            {
                Messenger.Send(new PlayMediaMessage(media, false));
            }
        }

        [RelayCommand]
        private void Remove(MediaViewModel media)
        {
            Recent.Remove(media);
            if (pathToMruMappings.Remove(media.Location, out string token))
            {
                StorageApplicationPermissions.MostRecentlyUsedList.Remove(token);
            }
        }

        [RelayCommand]
        private async Task OpenFolderAsync()
        {
            StorageFolder? folder = await filesService.PickFolderAsync();
            if (folder == null) return;
            IReadOnlyList<IStorageItem> items = await filesService.GetSupportedItems(folder).GetItemsAsync();
            IStorageFile[] files = items.OfType<IStorageFile>().ToArray();
            if (files.Length == 0) return;
            Messenger.Send(new PlayMediaMessage(files));
        }

        private static async Task<StorageFile?> ConvertMruTokenToStorageFileAsync(string token)
        {
            try
            {
                return await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(token,
                    AccessCacheOptions.SuppressAccessTimeUpdate);
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (Exception e)
            {
                LogService.Log(e);
                return null;
            }
        }
    }
}
