#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using VLC.Net.Core.Messages;
using VLC.Net.Core.Models;
using VLC.Net.Core.Services;

namespace VLC.Net.Core.Helpers
{
    public sealed class LastPositionTracker : ObservableRecipient,
        IRecipient<SuspendingMessage>
    {
        private const int Capacity = 64;
        private const string SaveFileName = "last_positions.bin";

        public bool IsLoaded => LastUpdated != default;

        public DateTimeOffset LastUpdated { get; private set; }

        private readonly IFilesService filesService;
        private List<MediaLastPosition> lastPositions = new(Capacity + 1);
        private MediaLastPosition? updateCache;
        private string? removeCache;

        public LastPositionTracker(IFilesService filesService)
        {
            this.filesService = filesService;

            IsActive = true;
        }

        public void Receive(SuspendingMessage message)
        {
            message.Reply(SaveToDiskAsync());
        }

        public void UpdateLastPosition(string location, TimeSpan position)
        {
            LastUpdated = DateTimeOffset.Now;
            removeCache = null;
            MediaLastPosition? item = updateCache;
            if (item?.Location == location)
            {
                item.Position = position;
                if (lastPositions.FirstOrDefault() != item)
                {
                    int index = lastPositions.IndexOf(item);
                    if (index >= 0)
                    {
                        lastPositions.RemoveAt(index);
                    }

                    lastPositions.Insert(0, item);
                }
            }
            else
            {
                item = lastPositions.Find(x => x.Location == location);
                if (item == null)
                {
                    item = new MediaLastPosition(location, position);
                    lastPositions.Insert(0, item);
                    if (lastPositions.Count > Capacity)
                    {
                        lastPositions.RemoveAt(Capacity);
                    }
                }
                else
                {
                    item.Position = position;
                }
            }

            updateCache = item;
        }

        public TimeSpan GetPosition(string location)
        {
            return lastPositions.Find(x => x.Location == location)?.Position ?? TimeSpan.Zero;
        }

        public void RemovePosition(string location)
        {
            LastUpdated = DateTimeOffset.Now;
            if (removeCache == location) return;
            lastPositions.RemoveAll(x => x.Location == location);
            removeCache = location;
        }

        public async Task SaveToDiskAsync()
        {
            try
            {
                await filesService.SaveToDiskAsync(ApplicationData.Current.TemporaryFolder, SaveFileName, lastPositions.ToList());
            }
            catch (FileLoadException)
            {
                // File in use. Skipped
            }
        }

        public async Task LoadFromDiskAsync()
        {
            try
            {
                List<MediaLastPosition> lastPositions =
                    await filesService.LoadFromDiskAsync<List<MediaLastPosition>>(ApplicationData.Current.TemporaryFolder, SaveFileName);
                lastPositions.Capacity = Capacity;
                this.lastPositions = lastPositions;
                LastUpdated = DateTimeOffset.UtcNow;
            }
            catch (FileNotFoundException)
            {
                // pass
            }
            catch (Exception)
            {
                // pass
            }
        }
    }
}
