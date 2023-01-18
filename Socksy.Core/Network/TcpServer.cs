using System.Runtime.CompilerServices;

namespace Socksy.Core.Network;

internal sealed class TcpServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly Func<int, Request, Task> _onConnectionEstablished;
    private CancellationTokenSource? _cancellationTokenSource;
    private CancellationToken _cancellationToken;
    private IPEndPoint _localEndPoint;
    private int _activeRequests;
    private bool _isListening;
    private int _reqNum = 0;

    public TcpServer(IPEndPoint localEndPoint, Func<int, Request, Task> onConnectionEstablished)
    {
        _localEndPoint = localEndPoint;
        _listener = new TcpListener(localEndPoint);
        _onConnectionEstablished = onConnectionEstablished;
        _isListening = false;
    }

    public bool IsListening => _isListening;

    public int ListeningPort => _localEndPoint.Port;
    public IPAddress ListeningAddress => _localEndPoint.Address;

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
        if (!_isListening)
            return;

        _listener.Stop();
        _isListening = false;
        _cancellationTokenSource?.Cancel();
        WaitForAllActiveConnectionRequestsToComplete().Wait();
    }

    private async Task MainLoop()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            var tcpClient = await _listener.AcceptTcpClientAsync(_cancellationToken);
            Interlocked.Add(ref _activeRequests, 1);
            Interlocked.Add(ref _reqNum, 1);

            _ = _onConnectionEstablished(_reqNum, Request.CreateFromTcpClient(tcpClient)).ContinueWith((t) =>
            {
                Interlocked.Add(ref _activeRequests, -1);
            });
        }
    }

    private async Task WaitForAllActiveConnectionRequestsToComplete()
    {
        while (_activeRequests > 0)
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
