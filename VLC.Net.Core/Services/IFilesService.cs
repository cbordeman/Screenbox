#nullable enable

using Avalonia.Controls;
using Avalonia.Platform.Storage;
using VLC.Net.Core.Models;

namespace VLC.Net.Core.Services
{
    public interface IFilesService
    {
        /// <summary>
        /// Within the user's profile, get the full path to the file.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        string GetProfileFileName(string filename);
        
        
        
        Task<StorageFileQueryResult?> GetNeighboringFilesQueryAsync(StorageFile file, QueryOptions? options = null);
        Task<StorageFile?> GetNextFileAsync(IStorageFile currentFile,
            StorageFileQueryResult neighboringFilesQuery);
        Task<StorageFile?> GetPreviousFileAsync(IStorageFile currentFile,
            StorageFileQueryResult neighboringFilesQuery);
        StorageItemQueryResult GetSupportedItems(StorageFolder folder);
        IAsyncOperation<uint> GetSupportedItemCountAsync(StorageFolder folder);
        IAsyncOperation<StorageFile> PickFileAsync(params string[] formats);
        Task<IReadOnlyList<IStorageFile>?> PickMultipleFilesAsync(params string[] formats);
        IAsyncOperation<StorageFolder> PickFolderAsync();
        Task OpenFileLocationAsync(string path);
        Task OpenFileLocationAsync(StorageFile file);
        void AddToRecent(IStorageItem item);
        Task<StorageFile> SaveToDiskAsync<T>(StorageFolder folder, string fileName, T source);
        Task SaveToDiskAsync<T>(StorageFile file, T source);
        Task<T> LoadFromDiskAsync<T>(StorageFolder folder, string fileName);
        Task<T> LoadFromDiskAsync<T>(StorageFile file);
        Task<MediaInfo> GetMediaInfoAsync(StorageFile file);
    }
}