namespace Socksy.Core.Models;

public struct Request
{
    public TcpClient TcpClient { get; internal init; }

    internal static Request CreateFromTcpClient(TcpClient client)
        => new Request { TcpClient = client };
}
