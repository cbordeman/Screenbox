#nullable enable

using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using VLC.Net.Core.Common;
using VLC.Net.Core.Factories;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.ViewModels;

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
        DateCreated = storageItem.GetDateCreated();

        if (storageItem is IStorageFile file)
        {
            IsFile = true;
            Media = mediaFactory.GetSingleton(file);
            Name = Media.Name;
            Path = Media.Location;
        }
        else
        {
            Name = storageItem.Name;
            Path = storageItem.Path.GetFilePath();
        }
    }

    public async Task UpdateCaptionAsync()
    {
        try
        {
            switch (StorageItem)
            {
                case IStorageFolder folder 
                    when !string.IsNullOrEmpty(folder.Path.GetFilePath()):
                    ItemCount = await filesService.GetSupportedItemCountAsync(folder);
                    break;
                case IStorageFile file:
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
                            await file.RetrievePropertiesAsync(additionalPropertyKeys);

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

public static class SystemProperties
{
  /// <summary>Gets the name of the System.Author property (one of the Windows file properties.</summary>
  /// <returns>The name of the System.Author file property.</returns>
  public static extern string Author { [MethodImpl(MethodCodeType = MethodCodeType.Runtime)] get; }
  /// <summary>Gets the name of the System.Comment property (one of the Windows file properties.</summary>
  /// <returns>The name of the System.Comment file property.</returns>
  public static extern string Comment { [MethodImpl(MethodCodeType = MethodCodeType.Runtime)] get; }
  /// <summary>Gets the name of the System.ItemNameDisplay property (one of the Windows file properties.</summary>
  /// <returns>The name of the System.ItemNameDisplay file property.</returns>
  public static extern string ItemNameDisplay { [MethodImpl(MethodCodeType = MethodCodeType.Runtime)] get; }
  /// <summary>Gets the name of the System.Keywords property (one of the Windows file properties.</summary>
  /// <returns>The name of the System.Keywords file property.</returns>
  public static extern string Keywords { [MethodImpl(MethodCodeType = MethodCodeType.Runtime)] get; }
  /// <summary>Gets the name of the System.Rating property (one of the Windows file properties.</summary>
  /// <returns>The name of the System.Rating file property.</returns>
  public static extern string Rating { [MethodImpl(MethodCodeType = MethodCodeType.Runtime)] get; }
  /// <summary>Gets the name of the System.Title property (one of the Windows file properties.</summary>
  /// <returns>The name of the System.Title file property.</returns>
  public static extern string Title { [MethodImpl(MethodCodeType = MethodCodeType.Runtime)] get; }
  /// <summary>Gets an object that provides the indexing names of Windows file properties for **System.Audio**.</summary>
  /// <returns>A helper object that provides names for Windows file properties for **System.Audio**.</returns>
  public static extern SystemAudioProperties Audio { [MethodImpl(MethodCodeType = MethodCodeType.Runtime)] get; }
  /// <summary>Gets an object that provides the indexing names of Windows system file properties for **System.GPS**.</summary>
  /// <returns>A helper object that provides names for GPS-related file properties.</returns>
  public static extern SystemGPSProperties GPS { [MethodImpl(MethodCodeType = MethodCodeType.Runtime)] get; }
  /// <summary>Gets an object that provides the indexing names of system media file properties such as System.Media.Duration.</summary>
  /// <returns>A helper object that provides names for system media file properties.</returns>
  public static extern SystemMediaProperties Media { [MethodImpl(MethodCodeType = MethodCodeType.Runtime)] get; }
  /// <summary>Gets an object that provides the indexing names of Windows file properties for **System.Music**.</summary>
  /// <returns>A helper object that provides names for Windows file properties for **System.Music**.</returns>
  public static extern SystemMusicProperties Music { [MethodImpl(MethodCodeType = MethodCodeType.Runtime)] get; }
  /// <summary>Gets an object that provides the indexing names of Windows file properties for **System.Photo**.</summary>
  /// <returns>A helper object that provides names for Windows file properties for **System.Photo**.</returns>
  public static extern SystemPhotoProperties Photo { [MethodImpl(MethodCodeType = MethodCodeType.Runtime)] get; }
  /// <summary>Gets an object that provides the indexing names of Windows file properties for **System.Video**.</summary>
  /// <returns>A helper object that provides names for Windows file properties for **System.Video**.</returns>
  public static extern SystemVideoProperties Video { [MethodImpl(MethodCodeType = MethodCodeType.Runtime)] get; }
  /// <summary>Gets an object that provides the indexing names of Windows file properties for **System.Image**.</summary>
  /// <returns>A helper object that provides names for Windows file properties for **System.Image**.</returns>
  public static extern SystemImageProperties Image { [MethodImpl(MethodCodeType = MethodCodeType.Runtime)] get; }
}
