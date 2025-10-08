using Avalonia.Platform.Storage;
using LibVLCSharp.Shared;
using VLC.Net.Core.Playback;
using VLC.Net.Core.Services;
using MediaViewModel = VLC.Net.Core.ViewModels.MediaViewModel;

namespace VLC.Net.Core.Factories
{
    public sealed class MediaViewModelFactory
    {
        private readonly LibVlcService libVlcService;
        private readonly Dictionary<string, WeakReference<MediaViewModel>> references = new();
        private int referencesCleanUpThreshold = 1000;

        public MediaViewModelFactory(LibVlcService libVlcService)
        {
            this.libVlcService = libVlcService;
        }

        public MediaViewModel GetTransient(IStorageFile file)
        {
            return new MediaViewModel(libVlcService, file);
        }

        public MediaViewModel GetTransient(Uri uri)
        {
            return new MediaViewModel(libVlcService, uri);
        }

        public MediaViewModel GetTransient(Media media)
        {
            if (!Uri.TryCreate(media.Mrl, UriKind.Absolute, out Uri uri))
                return new MediaViewModel(libVlcService, media);

            // Prefer URI source for easier clean up
            MediaViewModel vm = new(libVlcService, uri)
            {
                Item = new Lazy<PlaybackItem>(new PlaybackItem(media, media))
            };

            if (media.Meta(MetadataType.Title) is { } name && !string.IsNullOrEmpty(name))
                vm.Name = name;

            return vm;
        }

        public MediaViewModel GetSingleton(IStorageFile file)
        {
            string id = file.Path.GetFilePath();
            if (references.TryGetValue(id, out WeakReference<MediaViewModel> reference) &&
                reference.TryGetTarget(out MediaViewModel instance))
            {
                // Prefer storage file source
                if (instance.Source is not IStorageFile)
                {
                    instance.UpdateSource(file);
                }

                return instance;
            }


            // No existing reference, create new instance
            instance = new MediaViewModel(libVlcService, file);
            if (!string.IsNullOrEmpty(id))
            {
                references[id] = new WeakReference<MediaViewModel>(instance);
                CleanUpStaleReferences();
            }

            return instance;
        }

        public MediaViewModel GetSingleton(Uri uri)
        {
            string id = uri.OriginalString;
            if (references.TryGetValue(id, out WeakReference<MediaViewModel> reference) &&
                reference.TryGetTarget(out MediaViewModel instance)) return instance;

            // No existing reference, create new instance
            instance = new MediaViewModel(libVlcService, uri);
            if (!string.IsNullOrEmpty(id))
            {
                references[id] = new WeakReference<MediaViewModel>(instance);
                CleanUpStaleReferences();
            }

            return instance;
        }

        private void CleanUpStaleReferences()
        {
            if (references.Count < referencesCleanUpThreshold) return;
            string[] keysToRemove = references
                .Where(pair => !pair.Value.TryGetTarget(out MediaViewModel _))
                .Select(pair => pair.Key).ToArray();
            foreach (string key in keysToRemove)
            {
                references.Remove(key);
            }

            referencesCleanUpThreshold = Math.Max(references.Count * 2, referencesCleanUpThreshold);
        }
    }
}
