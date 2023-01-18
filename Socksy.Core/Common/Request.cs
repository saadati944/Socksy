namespace Socksy.Core.Server;

public struct Request
{
    public TcpClient TcpClient { get; internal init; }
    internal ISocket CreateSocket() => new SocketWrapper(TcpClient.Client);

    internal static Request CreateFromTcpClient(TcpClient client)
        => new Request { TcpClient = client };
}
