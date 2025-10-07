#nullable enable


using MediaViewModel = VLC.Net.Core.ViewModels.MediaViewModel;

namespace VLC.Net.Core.Common
{
    public interface IPropertiesDialog : IDialog
    {
        MediaViewModel? Media { get; set; }
    }
}
