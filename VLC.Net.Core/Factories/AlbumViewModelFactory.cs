#nullable enable

using System.Globalization;
using VLC.Net.Core.Enums;
using VLC.Net.Core.Services;
using VLC.Net.Core.ViewModels;
using MediaViewModel = VLC.Net.Core.ViewModels.MediaViewModel;

namespace VLC.Net.Core.Factories
{
    public sealed class AlbumViewModelFactory
    {
        public AlbumViewModel UnknownAlbum { get; }

        public IReadOnlyCollection<AlbumViewModel> AllAlbums { get; }

        private readonly Dictionary<string, AlbumViewModel> allAlbums;
        private readonly IResourceService resourceService;

        public AlbumViewModelFactory(IResourceService resourceService)
        {
            this.resourceService = resourceService;
            UnknownAlbum = new AlbumViewModel(resourceService.GetString(ResourceName.UnknownAlbum), resourceService.GetString(ResourceName.UnknownArtist));
            allAlbums = new Dictionary<string, AlbumViewModel>();
            AllAlbums = allAlbums.Values;
        }

        public AlbumViewModel GetAlbumFromName(string albumName, string artistName)
        {
            if (string.IsNullOrEmpty(albumName) || albumName == resourceService.GetString(ResourceName.UnknownAlbum))
            {
                return UnknownAlbum;
            }

            string albumKey = albumName.Trim().ToLower(CultureInfo.CurrentUICulture);
            string artistKey = artistName.Trim().ToLower(CultureInfo.CurrentUICulture);
            string key = GetAlbumKey(albumKey, artistKey);
            return allAlbums.GetValueOrDefault(key, UnknownAlbum);
        }

        public AlbumViewModel AddSongToAlbum(MediaViewModel song, string albumName, string artistName, uint year)
        {
            if (string.IsNullOrEmpty(albumName))
            {
                UnknownAlbum.RelatedSongs.Add(song);
                UpdateAlbumDateAdded(UnknownAlbum, song);
                return UnknownAlbum;
            }

            AlbumViewModel album = GetAlbumFromName(albumName, artistName);
            if (album != UnknownAlbum)
            {
                album.Year ??= year;
                album.RelatedSongs.Add(song);
                UpdateAlbumDateAdded(album, song);
                return album;
            }

            string albumKey = albumName.Trim().ToLower(CultureInfo.CurrentUICulture);
            string artistKey = artistName.Trim().ToLower(CultureInfo.CurrentUICulture);
            string key = GetAlbumKey(albumKey, artistKey);
            album = new AlbumViewModel(albumName, artistName)
            {
                Year = year
            };

            album.RelatedSongs.Add(song);
            UpdateAlbumDateAdded(album, song);
            return allAlbums[key] = album;
        }

        public void Remove(MediaViewModel song)
        {
            AlbumViewModel? album = song.Album;
            if (album == null) return;
            song.Album = null;
            album.RelatedSongs.Remove(song);
            if (album.RelatedSongs.Count == 0)
            {
                string albumKey = album.Name.Trim().ToLower(CultureInfo.CurrentUICulture);
                string artistKey = album.ArtistName.Trim().ToLower(CultureInfo.CurrentUICulture);
                allAlbums.Remove(GetAlbumKey(albumKey, artistKey));
            }
        }

        public void Compact()
        {
            List<string> albumKeysToRemove =
                allAlbums.Where(p => p.Value.RelatedSongs.Count == 0).Select(p => p.Key).ToList();

            foreach (string albumKey in albumKeysToRemove)
            {
                allAlbums.Remove(albumKey);
            }
        }

        public void Clear()
        {
            foreach (MediaViewModel media in UnknownAlbum.RelatedSongs)
            {
                media.Album = null;
            }

            UnknownAlbum.RelatedSongs.Clear();
            UnknownAlbum.DateAdded = default;

            foreach ((string _, AlbumViewModel album) in allAlbums)
            {
                foreach (MediaViewModel media in album.RelatedSongs)
                {
                    media.Album = null;
                }
            }

            allAlbums.Clear();
        }

        private static void UpdateAlbumDateAdded(AlbumViewModel album, MediaViewModel song)
        {
            if (song.DateAdded == default) return;
            if (album.DateAdded > song.DateAdded || album.DateAdded == default) album.DateAdded = song.DateAdded;
        }

        private static string GetAlbumKey(string albumName, string artistName)
        {
            return $"{albumName};{artistName}";
        }
    }
}
