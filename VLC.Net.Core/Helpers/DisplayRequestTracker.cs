namespace VLC.Net.Core.Helpers
{
    internal class DisplayRequestTracker
    {
        public bool IsActive => requestCount > 0;

        private readonly DisplayRequest displayRequest;
        private int requestCount;

        public DisplayRequestTracker()
        {
            displayRequest = new DisplayRequest();
        }

        public void RequestActive()
        {
            lock (displayRequest)
            {
                try
                {
                    displayRequest.RequestActive();
                    requestCount++;
                }
                catch (Exception)
                {
                    // pass
                }
            }
        }

        public void RequestRelease()
        {
            lock (displayRequest)
            {
                if (requestCount <= 0) return;
                try
                {
                    displayRequest.RequestRelease();
                    requestCount--;
                }
                catch (Exception)
                {
                    // pass
                }
            }
        }
    }
}
