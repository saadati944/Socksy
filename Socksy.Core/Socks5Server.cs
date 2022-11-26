using System.Runtime.CompilerServices;

namespace Socksy.Core;

public sealed class Socks5Server : IDisposable
{
    private readonly TcpServer _server;
    private readonly Action<Request>? _onClientConnected;
    
    public Socks5Server(IPEndPoint localEndPoint, Action<Request>? onClientConnected = null)
    {
        _server = new TcpServer(localEndPoint, OnConnectionEstablished);
        _onClientConnected = onClientConnected;
    }

    public bool IsListening => _server.IsListening;

    public void Start()
    {
        _server.Start();
    }

    public void Stop()
    {
        _server.Stop();
    }

    private async Task OnConnectionEstablished(TcpClient tcpClient)
    {
        var request = Request.CreateFromTcpClient(tcpClient);

        await Task.CompletedTask;

        if (_onClientConnected is not null)
            _onClientConnected(request);
    }

    public TaskAwaiter GetAwaiter()
    {
        return _server.GetAwaiter();
    }

    public void Dispose()
    {
        Stop();
        _server.Dispose();
    }
}
