using System.Runtime.CompilerServices;

namespace Socksy.Core.Network;

internal sealed class TcpServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly Func<TcpClient, Task> _onConnectionEstablished;
    private CancellationTokenSource? _cancellationTokenSource;
    private CancellationToken _cancellationToken;
    private int _activeRequests;
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
        if(!_isListening)
            return;

        _listener.Stop();
        WaitForAllActiveConnectionRequestsToComplete().Wait();
        _isListening = false;
        _cancellationTokenSource?.Cancel();
    }

    private async Task MainLoop()
    {
        while(!_cancellationToken.IsCancellationRequested)
        {
            var tcpClient = await _listener.AcceptTcpClientAsync(_cancellationToken);
            Interlocked.Add(ref _activeRequests, 1);

            _ = _onConnectionEstablished(tcpClient).ContinueWith((t) => {
                Interlocked.Add(ref _activeRequests, -1);
            });
        }
    }

    private async Task WaitForAllActiveConnectionRequestsToComplete()
    {
        while(_activeRequests > 0)
        {
            await Task.Delay(10);
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
