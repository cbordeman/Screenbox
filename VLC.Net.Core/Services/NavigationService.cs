#nullable enable

using VLC.Net.Core.Common;

namespace VLC.Net.Core.Services
{
    public sealed class NavigationService : INavigationService
    {
        public event EventHandler? Navigated;

        private readonly Dictionary<Type, Type> vmPageMapping;

        public NavigationService(params KeyValuePair<Type, Type>[] mapping)
        {
            vmPageMapping = new Dictionary<Type, Type>(mapping);
        }

        public bool TryGetPageType(Type vmType, out Type pageType)
        {
            return vmPageMapping.TryGetValue(vmType, out pageType);
        }

        public void Navigate(Type vmType, object? parameter = null)
        {
            if (!vmPageMapping.TryGetValue(vmType, out Type pageType)) return;

            Frame rootFrame = (Frame)Window.Current.Content;
            if (rootFrame.Content is IContentFrame page)
            {
                page.NavigateContent(pageType, parameter);
                Navigated?.Invoke(this, EventArgs.Empty);
            }
        }

        public void NavigateChild(Type parentVmType, Type targetVmType, object? parameter = null)
        {
            if (!vmPageMapping.TryGetValue(parentVmType, out Type parentPageType)) return;
            if (!vmPageMapping.TryGetValue(targetVmType, out Type targetPageType)) return;

            Frame rootFrame = (Frame)Window.Current.Content;
            IContentFrame? page = rootFrame.Content as IContentFrame;
            while (page != null)
            {
                if (page.ContentSourcePageType == parentPageType && page.FrameContent is IContentFrame childPage)
                {
                    childPage.NavigateContent(targetPageType, parameter);
                    Navigated?.Invoke(this, EventArgs.Empty);
                    return;
                }

                page = page.FrameContent as IContentFrame;
            }
        }

        public void NavigateExisting(Type vmType, object? parameter = null)
        {
            if (!vmPageMapping.TryGetValue(vmType, out Type pageType)) return;

            Frame rootFrame = (Frame)Window.Current.Content;
            IContentFrame? page = rootFrame.Content as IContentFrame;
            while (page != null)
            {
                if (page.ContentSourcePageType == pageType)
                {
                    page.NavigateContent(pageType, parameter);
                    Navigated?.Invoke(this, EventArgs.Empty);
                    break;
                }

                page = page.FrameContent as IContentFrame;
            }
        }
    }
}
