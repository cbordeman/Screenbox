using Avalonia.Platform.Storage;
using VLC.Net.Core.Infrastructure;
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
        
        
        
        Task<StorageFileQueryResult?> GetNeighboringFilesQueryAsync(IStorageFile file, QueryOptions? options = null);
        Task<IStorageFile?> GetNextFileAsync(IStorageFile currentFile,
            StorageFileQueryResult neighboringFilesQuery);
        Task<IStorageFile?> GetPreviousFileAsync(IStorageFile currentFile,
            StorageFileQueryResult neighboringFilesQuery);
        StorageItemQueryResult GetSupportedItems(IStorageFolder folder);
        Task<uint> GetSupportedItemCountAsync(IStorageFolder folder);
        Task<IStorageFile> PickFileAsync(params string[] formats);
        Task<IReadOnlyList<IStorageFile>?> PickMultipleFilesAsync(params string[] formats);
        Task<IStorageFolder> PickFolderAsync();
        Task OpenFileLocationAsync(string path);
        Task OpenFileLocationAsync(IStorageFile file);
        void AddToRecent(IStorageItem item);
        Task<IStorageFile> SaveToDiskAsync<T>(IStorageFolder folder, string fileName, T source);
        Task SaveToDiskAsync<T>(IStorageFile file, T source);
        Task<T> LoadFromDiskAsync<T>(IStorageFolder folder, string fileName);
        Task<T> LoadFromDiskAsync<T>(IStorageFile file);
        Task<MediaInfo> GetMediaInfoAsync(IStorageFile file);
    }
}