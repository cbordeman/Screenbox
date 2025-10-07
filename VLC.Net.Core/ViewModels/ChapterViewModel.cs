using CommunityToolkit.Mvvm.ComponentModel;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class ChapterViewModel : ObservableObject
    {
        [ObservableProperty]
        private double value;

        [ObservableProperty]
        private double minimum;

        [ObservableProperty]
        private double maximum;

        [ObservableProperty]
        private double width;
    }
}
