#nullable enable

namespace VLC.Net.Core.Messages;
public class DragDropMessage
{
    public DataPackageView Data { get; }

    public DragDropMessage(DataPackageView data)
    {
        Data = data;
    }
}
