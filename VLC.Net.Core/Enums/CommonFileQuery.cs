namespace VLC.Net.Core.Enums;

public enum CommonFileQuery
{
    /// <summary>A shallow list of files in the current folder, similar to the list that File Explorer provides.</summary>
    DefaultQuery,
    /// <summary>A deep, flat list of files in a folder and its subfolders, sorted by System.ItemNameDisplay.</summary>
    OrderByName,
    /// <summary>A deep, flat list of files in a folder and its subfolders, sorted by System.Title.</summary>
    OrderByTitle,
    /// <summary>A deep, flat list of files in a folder and its subfolders, sorted by music properties.</summary>
    OrderByMusicProperties,
    /// <summary>A deep, flat list of files in a folder and its subfolders, sorted by System.Search.Rank followed by System.DateModified.</summary>
    OrderBySearchRank,
    /// <summary>A deep, flat list of files in a folder and its subfolders, sorted by System.ItemDate.</summary>
    OrderByDate,
}

