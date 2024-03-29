using Socksy.Core.Common;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Socksy.Core;

public sealed class Socks5Server : IDisposable
{
    private const int VER = 5;

    private readonly TcpServer _server;
    private readonly Action<Request>? _onClientConnected;
    private readonly Action<int, string>? _log;
    private Regex[]? _blackListRegexes = null;

    private readonly ConcurrentDictionary<int, ConnectionState> _activeConnections;
    private long _inCounter;
    private long _outCounter;

    public Socks5Server(IPEndPoint localEndPoint, Action<Request>? onClientConnected = null, Action<int, string>? logFunction = null, ServerOptions? options = null)
    {
        _server = new TcpServer(localEndPoint, OnConnectionEstablished);
        _onClientConnected = onClientConnected;
        _activeConnections = new ConcurrentDictionary<int, ConnectionState>();
        _log = logFunction;

        if (options is not null)
            InitializeOptions(options);
    }

    private void InitializeOptions(ServerOptions options)
    {
        if (options.BlockedAddresses is not null && options.BlockedAddresses.Length > 0)
        {
            Log(0, () => $"Initializing black list with {options.BlockedAddresses.Length} items");
            _blackListRegexes = new Regex[options.BlockedAddresses.Length];
            for (int i = 0; i < options.BlockedAddresses.Length; i++)
                _blackListRegexes[i] = new Regex(options.BlockedAddresses[i], RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
        }
    }

    public bool IsListening => _server.IsListening;

    public long OutGoingBytes => _outCounter;
    public long InCommingBytes => _inCounter;
    public IReadOnlyDictionary<int, ConnectionState> ActiveConnections => _activeConnections;

    public void Start()
    {
        _server.Start();
    }

    public void Stop()
    {
        _server.Stop();
    }

    private async Task OnConnectionEstablished(int reqNum, Request request)
    {
        try
        {
            _onClientConnected?.Invoke(request);
            var socket = request.CreateSocket();

            Log(reqNum, () => "Waiting for MethodSelector ...");
            var method = MethodSelectorDTO.GetFromSocket(socket);
            if (method is null || method.VER != 5 || !method.METHODS!.Contains((byte)AuthenticationMETHOD.NO_AUTHENTICATION_REQUIRED))
                throw new Exception("Bad request, client does not support 'NO AUTHENTICATION REQUIRED' method");
            Log(reqNum, () => $"MethodSelector received. ver: {method.VER}, nmethods: {method.NMETHODS}, methods: '{string.Join(", ", method.METHODS!)}'");
            _activeConnections[reqNum] = ConnectionState.MethodSelectorReceived;

            Log(reqNum, () => "Sending SetMethodDTO");
            var sm = SetMethodDTO.Create(VER, AuthenticationMETHOD.NO_AUTHENTICATION_REQUIRED);
            sm.Send(socket);
            Log(reqNum, () => $"SetMethod sent. ver: {sm.VER}, method: {sm.METHOD}");
            _activeConnections[reqNum] = ConnectionState.SetMethodSent;

            Log(reqNum, () => "Waiting for RequestDTO ...");
            var req = RequestDTO.GetFromSocket(socket);
            Log(reqNum, () => $"RequestDTO received. VER: {req.VER}, CMD: {req.CMD}, RSV: {req.RSV}, ATYPE: {req.ATYPE}, DST_ADDR: {req.DST_ADDR}, DST_ADDR_STRING: {req.DST_ADDR_STRING}, DST_ADDR_IPADDRESS: {req.DST_ADDR_IPADDRESS}");
            _activeConnections[reqNum] = ConnectionState.RequestReceived;

            if (req.CMD == RequestCMD.CONNECT)
            {
                if (AddressIsInBlackList(req.DST_ADDR_STRING))
                {
                    Log(reqNum, () => "Requested address was in black list");
                    var blockedRep = ReplyDTO.Create(
                        VER,
                        ReplyREP.Connection_not_allowed_by_ruleset,
                        _server.ListeningAddress.AddressFamily == AddressFamily.InterNetwork ? AddressTYPE.IPV4 : AddressTYPE.IPV6,
                        _server.ListeningAddress.GetAddressBytes(),
                        (ushort)_server.ListeningPort);
                    blockedRep.Send(socket);
                    Log(reqNum, () => $"Not-Allowed Reply Sent. VER: {blockedRep.VER}, REP: {blockedRep.REP}, RSV: {blockedRep.RSV}, ATYPE: {blockedRep.ATYPE}, BND_ADDR: {blockedRep.BND_ADDR}, BND_ADDR_STRING: {blockedRep.BND_ADDR_STRING}");
                    return;
                }

                IPEndPoint remote_endpoint = new IPEndPoint(
                    req.ATYPE switch
                    {
                        AddressTYPE.IPV4 or AddressTYPE.IPV6 => new IPAddress(req.DST_ADDR!),
                        _ => Dns.GetHostAddresses(req.DST_ADDR_STRING, AddressFamily.InterNetwork)[0],
                    },
                    req.DST_PORT);
                Log(reqNum, () => $"Connecting to remote endpoint. IP address: {remote_endpoint.Address}, port: {remote_endpoint.Port}");
                _activeConnections[reqNum] = ConnectionState.Connecting;

                TcpClient remoteClient = new TcpClient();
                await remoteClient.ConnectAsync(remote_endpoint);
                var remote = new SocketWrapper(remoteClient.Client);
                Log(reqNum, () => $"Connected: {remoteClient.Connected}");

                Log(reqNum, () => "Sending Reply");
                var rep = ReplyDTO.Create(
                    VER,
                    ReplyREP.Succeeded,
                    _server.ListeningAddress.AddressFamily == AddressFamily.InterNetwork ? AddressTYPE.IPV4 : AddressTYPE.IPV6,
                    _server.ListeningAddress.GetAddressBytes(),
                    (ushort)_server.ListeningPort);
                rep.Send(socket);
                Log(reqNum, () => $"Reply Sent. VER: {rep.VER}, REP: {rep.REP}, RSV: {rep.RSV}, ATYPE: {rep.ATYPE}, BND_ADDR: {rep.BND_ADDR}, BND_ADDR_STRING: {rep.BND_ADDR_STRING}");

                Log(reqNum, () => "Exchanging data ...");
                _activeConnections[reqNum] = ConnectionState.ExchangingData;
                await ExchangeData(reqNum, remote, socket);
            }
            // else if(req.CMD == ...
            else
            {
                ReplyDTO.Create(
                    VER,
                    ReplyREP.Command_not_supported,
                    _server.ListeningAddress.AddressFamily == AddressFamily.InterNetwork ? AddressTYPE.IPV4 : AddressTYPE.IPV6,
                    _server.ListeningAddress.GetAddressBytes(),
                    (ushort)_server.ListeningPort);
                throw new Exception("Request command is not supported");
            }
        }
        finally
        {
            _ = _activeConnections.TryRemove(reqNum, out var _);
        }
    }

    private bool AddressIsInBlackList(string destinationAddress)
    {
        if (_blackListRegexes is null || _blackListRegexes.Length == 0)
            return false;

        for (int i = 0; i < _blackListRegexes.Length; i++)
            if (_blackListRegexes[i].IsMatch(destinationAddress))
                return true;

        return false;
    }

    private async Task ExchangeData(int reqNum, ISocket remote, ISocket client)
    {
        remote.SendTimeout = 20000;
        remote.ReceiveTimeout = 20000;
        client.SendTimeout = 20000;
        client.ReceiveTimeout = 20000;

        var t1 = MakeBridge(reqNum, remote, client, (i) => Interlocked.Add(ref _inCounter, i));
        var t2 = MakeBridge(reqNum, client, remote, (i) => Interlocked.Add(ref _outCounter, i));

        await Task.WhenAny(t1, t2);
        remote.Close();
        client.Close();
    }

    private async Task MakeBridge(int reqNum, ISocket from, ISocket to, Action<int>? onAfterSendingData)
    {
        Log(reqNum, () => $"Making bridge from {from.RemoteEndPoint.Address}:{from.RemoteEndPoint.Port} to {to.RemoteEndPoint.Address}:{to.RemoteEndPoint.Port}");

        try
        {
            int fails = 50;
            while (true)
            {
                var avail = from.Available;
                if (avail == 0)
                {
                    if ((!from.Poll(3000, SelectMode.SelectRead) && fails-- <= 0) || !from.Connected || !to.Connected)
                        break;
                    await Task.Delay(5);
                    continue;
                }
                fails = 50;
                var buf = ArrayPool<byte>.Shared.Rent(avail);
                from.Receive(new Span<byte>(buf, 0, avail));
                to.Send(new Span<byte>(buf, 0, avail));
                onAfterSendingData?.Invoke(avail);
            }
        }
        catch
        {
        }
        Log(reqNum, () => $"Bridge from {from.RemoteEndPoint.Address}:{from.RemoteEndPoint.Port} to {to.RemoteEndPoint.Address}:{to.RemoteEndPoint.Port} collapsed ...");
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

    [Conditional("DEBUG")]
    private void Log(int requestNumber, Func<string> message)
        => _log?.Invoke(requestNumber, message());
}

public enum ConnectionState
{
    MethodSelectorReceived,
    SetMethodSent,
    RequestReceived,
    Connecting,
    ExchangingData,
}