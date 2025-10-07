﻿#nullable enable

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace VLC.Net.Core.Messages;

public sealed class PlayFilesMessage : ValueChangedMessage<IReadOnlyList<IStorageItem>>
{
    public StorageFileQueryResult? NeighboringFilesQuery { get; }

    public PlayFilesMessage(IReadOnlyList<IStorageItem> files) : base(files) { }

    public PlayFilesMessage(IReadOnlyList<IStorageItem> files,
        StorageFileQueryResult? neighboringFilesQuery) : base(files)
    {
        NeighboringFilesQuery = neighboringFilesQuery;
    }
}