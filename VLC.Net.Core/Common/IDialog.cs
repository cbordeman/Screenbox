namespace VLC.Net.Core.Common
{
    public interface IDialog
    {
        object Title { get; set; }
        ContentDialogButton DefaultButton { get; set; }
        Task<ContentDialogResult> ShowAsync();
    }
}
