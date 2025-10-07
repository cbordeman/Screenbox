#nullable enable

using System.Globalization;
using VLC.Net.Core.Enums;
using VLC.Net.Core.Services;
using VLC.Net.Core.ViewModels;
using MediaViewModel = VLC.Net.Core.ViewModels.MediaViewModel;

namespace VLC.Net.Core.Factories
{
    public sealed class ArtistViewModelFactory
    {
        public ArtistViewModel UnknownArtist { get; }

        public IReadOnlyCollection<ArtistViewModel> AllArtists { get; }

        private readonly Dictionary<string, ArtistViewModel> allArtists;

        private static readonly string[] ArtistNameSeparators = { ",", ", ", "; " };

        public ArtistViewModelFactory(IResourceService resourceService)
        {
            allArtists = new Dictionary<string, ArtistViewModel>();
            AllArtists = allArtists.Values;
            UnknownArtist = new ArtistViewModel(resourceService.GetString(ResourceName.UnknownArtist));
        }

        public ArtistViewModel[] ParseArtists(string artist)
        {
            ArtistViewModel[] artists = artist.Split(ArtistNameSeparators, StringSplitOptions.RemoveEmptyEntries)
                .Select(GetArtistFromName)
                .ToArray();

            return artists.Length == 0 ? new[] { UnknownArtist } : artists;
        }

        public ArtistViewModel[] ParseAddArtists(string artist, MediaViewModel song)
        {
            return ParseAddArtists(artist.Split(ArtistNameSeparators, StringSplitOptions.RemoveEmptyEntries), song);
        }

        private ArtistViewModel[] ParseAddArtists(string[] artists, MediaViewModel song)
        {
            if (artists.Length == 0)
            {
                AddSongToArtist(song, string.Empty);
                return new[] { UnknownArtist };
            }

            IEnumerable<string> artistNames = artists;
            if (artists.Length == 1)
            {
                string artistName = artists[0];
                string[] splits = artistName.Split(ArtistNameSeparators, StringSplitOptions.RemoveEmptyEntries);
                if (splits.Length > 1)
                {
                    artistNames = splits.Prepend(artistName);
                }
            }

            return artistNames
                .Select(artist => AddSongToArtist(song, artist.Trim()))
                .ToArray();
        }

        public ArtistViewModel GetArtistFromName(string artistName)
        {
            if (string.IsNullOrEmpty(artistName))
                return UnknownArtist;

            string key = artistName.Trim().ToLower(CultureInfo.CurrentUICulture);
            return allArtists.GetValueOrDefault(key, UnknownArtist);
        }

        public ArtistViewModel AddSongToArtist(MediaViewModel song, string artistName)
        {
            if (string.IsNullOrEmpty(artistName))
            {
                UnknownArtist.RelatedSongs.Add(song);
                return UnknownArtist;
            }

            ArtistViewModel artist = GetArtistFromName(artistName);
            if (artist != UnknownArtist)
            {
                artist.RelatedSongs.Add(song);
                return artist;
            }

            string key = artistName.Trim().ToLower(CultureInfo.CurrentUICulture);
            artist = new ArtistViewModel(artistName);
            artist.RelatedSongs.Add(song);
            return allArtists[key] = artist;
        }

        public void Remove(MediaViewModel song)
        {
            foreach (ArtistViewModel artist in song.Artists)
            {
                artist.RelatedSongs.Remove(song);
                if (artist.RelatedSongs.Count == 0)
                {
                    string artistKey = artist.Name.Trim().ToLower(CultureInfo.CurrentUICulture);
                    allArtists.Remove(artistKey);
                }
            }

            song.Artists = Array.Empty<ArtistViewModel>();
        }

        public void Compact()
        {
            List<string> albumKeysToRemove =
                allArtists.Where(p => p.Value.RelatedSongs.Count == 0).Select(p => p.Key).ToList();

            foreach (string albumKey in albumKeysToRemove)
            {
                allArtists.Remove(albumKey);
            }
        }

        public void Clear()
        {
            foreach (MediaViewModel media in UnknownArtist.RelatedSongs)
            {
                media.Artists = Array.Empty<ArtistViewModel>();
            }

            UnknownArtist.RelatedSongs.Clear();

            foreach ((string _, ArtistViewModel artist) in allArtists)
            {
                foreach (MediaViewModel media in artist.RelatedSongs)
                {
                    media.Artists = Array.Empty<ArtistViewModel>();
                }
            }

            allArtists.Clear();
        }
    }
}
