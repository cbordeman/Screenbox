#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using VLC.Net.Core.Messages;

namespace VLC.Net.Core.ViewModels;
public abstract partial class BaseMusicContentViewModel : ObservableRecipient
{
    private bool HasSongs => Songs.Count > 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(Screenbox.Core.ViewModels.BaseMusicContentViewModel.ShuffleAndPlayCommand))]
    private IReadOnlyList<MediaViewModel> songs = Array.Empty<MediaViewModel>();

    [ObservableProperty] private bool isLoading;

    [RelayCommand(CanExecute = nameof(HasSongs))]
    private void ShuffleAndPlay()
    {
        if (Songs.Count == 0) return;
        Random rnd = new();
        List<MediaViewModel> shuffledList = Enumerable.OrderBy<MediaViewModel, int>(Songs, _ => rnd.Next()).ToList();
        Messenger.Send(new ClearPlaylistMessage());
        Messenger.Send(new QueuePlaylistMessage(shuffledList));
        Messenger.Send(new PlayMediaMessage(shuffledList[0], true));
    }
}
