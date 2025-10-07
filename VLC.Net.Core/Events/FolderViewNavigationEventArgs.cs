namespace VLC.Net.Core.Events
{
    public sealed class FolderViewNavigationEventArgs : EventArgs
    {
        public IReadOnlyList<StorageFolder> Breadcrumbs { get; }

        public FolderViewNavigationEventArgs(IReadOnlyList<StorageFolder> breadcrumbs)
        {
            Breadcrumbs = breadcrumbs;
        }
    }
}
