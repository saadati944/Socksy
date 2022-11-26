using Socksy.Core.Network;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Socksy.Core;
public class Socks5Server : IDisposable
{
    private readonly TcpServer _server;

    public Socks5Server(IPEndPoint localEndPoint)
    {
        _server = new TcpServer(localEndPoint, OnConnectionEstablished);
    }

    public void Start()
    {
        _server.Start();
    }

    public void Stop()
    {
        _server.Stop();
    }

    public bool IsListening => _server.IsListening;

    private async Task OnConnectionEstablished(TcpClient tcpClient)
    {
        await Task.CompletedTask;
    }

    public TaskAwaiter GetAwaiter()
    {
        return _server.GetAwaiter();
    }


    public void Dispose()
    {
        _server.Dispose();
    }
}
