using VLC.Net.Core.Models;

namespace VLC.Net.Core.Services
{
    public interface ISearchService
    {
        SearchResult SearchLocalLibrary(string query);
    }
}