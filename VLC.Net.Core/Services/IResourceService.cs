using VLC.Net.Core.Enums;

namespace VLC.Net.Core.Services
{
    public interface IResourceService
    {
        string GetString(ResourceName name, params object[] parameters);
    }
}
