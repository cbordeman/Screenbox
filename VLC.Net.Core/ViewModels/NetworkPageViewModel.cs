#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using VLC.Net.Core.Common;

namespace VLC.Net.Core.ViewModels
{
    public sealed partial class NetworkPageViewModel : ObservableRecipient
    {
        [ObservableProperty] private string titleText;

        public NetworkPageViewModel()
        {
            titleText = string.Empty;
            Breadcrumbs = new ObservableCollection<string>();
        }

        public ObservableCollection<string> Breadcrumbs { get; }

        public void OnNavigatedTo(object? parameter)
        {
            switch (parameter)
            {
                case NavigationMetadata { Parameter: IReadOnlyList<StorageFolder> crumbs }:
                    UpdateBreadcrumbs(crumbs);
                    break;
                case IReadOnlyList<StorageFolder> crumbs:
                    UpdateBreadcrumbs(crumbs);
                    break;
            }
        }

        private void UpdateBreadcrumbs(IReadOnlyList<StorageFolder>? crumbs)
        {
            Breadcrumbs.Clear();
            if (crumbs == null) return;
            TitleText = crumbs.LastOrDefault()?.DisplayName ?? string.Empty;
            foreach (StorageFolder storageFolder in crumbs)
            {
                Breadcrumbs.Add(storageFolder.DisplayName);
            }
        }
    }
}
