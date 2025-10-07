﻿namespace VLC.Net.Core.Models;

[ProtoContract]
internal class PersistentStorageLibrary
{
    [ProtoMember(1)] public List<string> FolderPaths { get; set; } = new();

    [ProtoMember(2)] public List<PersistentMediaRecord> Records { get; set; } = new();
}
