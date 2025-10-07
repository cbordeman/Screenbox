﻿namespace VLC.Net.Core.Messages
{
    public sealed class RaiseLibraryAccessDeniedNotificationMessage
    {
        public KnownLibraryId Library { get; }

        public RaiseLibraryAccessDeniedNotificationMessage(KnownLibraryId library)
        {
            Library = library;
        }
    }
}
