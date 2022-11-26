using System.Runtime.CompilerServices;

namespace Socksy.Core.Network;

internal class TcpServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly Func<TcpClient, Task> _onConnectionEstablished;
    private CancellationTokenSource? _cancellationTokenSource;
    private CancellationToken _cancellationToken;
    private bool _isListening;

    public TcpServer(IPEndPoint localEndPoint, Func<TcpClient, Task> onConnectionEstablished)
    {
        _listener = new TcpListener(localEndPoint);
        _onConnectionEstablished = onConnectionEstablished;
        _isListening = false;
    }

    public bool IsListening => _isListening;

    public void Start()
    {
        if (_isListening)
            return;

        _cancellationTokenSource = new CancellationTokenSource();
        _cancellationToken = _cancellationTokenSource.Token;
        _listener.Start();
        _ = MainLoop();
        _isListening = true;
    }

    public void Stop()
    {
        _cancellationTokenSource!.Cancel();
        _listener.Stop();
        _isListening = false;
    }

    private async Task MainLoop()
    {
        while(!_cancellationToken.IsCancellationRequested)
        {
            var tcpClient = await _listener.AcceptTcpClientAsync(_cancellationToken);
            _ = _onConnectionEstablished(tcpClient);
        }
    }

    public TaskAwaiter GetAwaiter()
    {
        TaskCompletionSource tcs = new TaskCompletionSource();
        _cancellationToken.Register(() => tcs.TrySetResult());
        return tcs.Task.GetAwaiter();
    }

    public void Dispose()
    {
        Stop();
    }
}
