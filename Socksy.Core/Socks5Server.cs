using Socksy.Core.Commands;
using Socksy.Core.Common;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Socksy.Core;

public sealed class Socks5Server : IDisposable
{
    // private const int VER = 5;

    // private readonly TcpServer _server;
    // private readonly Action<Request>? _onClientConnected;
    // private readonly Action<int, string>? _log;
    // private Regex[]? _blackListRegexes = null;

    // private readonly ConcurrentDictionary<int, ConnectionState> _activeConnections;
    // private long _inCounter;
    // private long _outCounter;

    public Socks5Server(IPEndPoint localEndPoint, Action<Request>? onClientConnected = null, Action<int, string>? logFunction = null, ServerOptions? options = null)
    {
        Configs._server = new TcpServer(localEndPoint, OnConnectionEstablished);
        Configs._onClientConnected = onClientConnected;
        Configs._activeConnections = new ConcurrentDictionary<int, ConnectionState>();
        Configs._log = logFunction;

        if (options is not null)
            InitializeOptions(options);
    }

    private void InitializeOptions(ServerOptions options)
    {
        if (options.BlockedAddresses is not null && options.BlockedAddresses.Length > 0)
        {
            Configs.Log(0, $"Initializing black list with {options.BlockedAddresses.Length} items");
            Configs._blackListRegexes = new Regex[options.BlockedAddresses.Length];
            for (int i = 0; i < options.BlockedAddresses.Length; i++)
                Configs._blackListRegexes[i] = new Regex(options.BlockedAddresses[i], RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
        }
    }

    public bool IsListening => Configs._server.IsListening;

    public long OutGoingBytes => Configs._outCounter;
    public long InCommingBytes => Configs._inCounter;
    public IReadOnlyDictionary<int, ConnectionState> ActiveConnections => Configs._activeConnections;

    public void Start()
    {
        Configs._server.Start();
    }

    public void Stop()
    {
        Configs._server.Stop();
    }

    private async Task OnConnectionEstablished(int reqNum, Request request)
    {
        try
        {
            Configs._onClientConnected?.Invoke(request);
            var socket = request.CreateSocket();

            GetMethodSelector(reqNum, socket);
            SendSetMethod(reqNum, socket);
            RequestDTO req = GetRequest(reqNum, socket);

            if (req.CMD == RequestCMD.CONNECT)
            {
                await ConnectCommand.ExecuteConnect(reqNum, socket, req);
            }
            // else if(req.CMD == ...
            else
            {
                ReplyDTO.Create(
                    Configs.VER,
                    ReplyREP.Command_not_supported,
                    Configs._server.ListeningAddress.AddressFamily == AddressFamily.InterNetwork ? AddressTYPE.IPV4 : AddressTYPE.IPV6,
                    Configs._server.ListeningAddress.GetAddressBytes(),
                    (ushort)Configs._server.ListeningPort);
                throw new Exception("Request command is not supported");
            }
        }
        finally
        {
            _ = Configs._activeConnections.TryRemove(reqNum, out var _);
        }
    }

    private RequestDTO GetRequest(int reqNum, ISocket socket)
    {
        Configs.Log(reqNum, "Waiting for RequestDTO ...");
        var req = RequestDTO.GetFromSocket(socket);
        Configs.Log(reqNum, $"RequestDTO received. VER: {req.VER}, CMD: {req.CMD}, RSV: {req.RSV}, ATYPE: {req.ATYPE}, DST_ADDR: {req.DST_ADDR}, DST_ADDR_STRING: {req.DST_ADDR_STRING}, DST_ADDR_IPADDRESS: {req.DST_ADDR_IPADDRESS}");
        Configs._activeConnections[reqNum] = ConnectionState.RequestReceived;
        return req;
    }

    private void SendSetMethod(int reqNum, ISocket socket)
    {
        Configs.Log(reqNum, "Sending SetMethodDTO");
        var sm = SetMethodDTO.Create(Configs.VER, AuthenticationMETHOD.NO_AUTHENTICATION_REQUIRED);
        sm.Send(socket);
        Configs.Log(reqNum, $"SetMethod sent. ver: {sm.VER}, method: {sm.METHOD}");
        Configs._activeConnections[reqNum] = ConnectionState.SetMethodSent;
    }

    private void GetMethodSelector(int reqNum, ISocket socket)
    {
        Configs.Log(reqNum, "Waiting for MethodSelector ...");
        var method = MethodSelectorDTO.GetFromSocket(socket);
        if (method is null || method.VER != 5 || !method.METHODS!.Contains((byte)AuthenticationMETHOD.NO_AUTHENTICATION_REQUIRED))
            throw new Exception("Bad request, client does not support 'NO AUTHENTICATION REQUIRED' method");
        Configs.Log(reqNum, $"MethodSelector received. ver: {method.VER}, nmethods: {method.NMETHODS}, methods: '{string.Join(", ", method.METHODS!)}'");
        Configs._activeConnections[reqNum] = ConnectionState.MethodSelectorReceived;
    }

    public TaskAwaiter GetAwaiter()
    {
        return Configs._server.GetAwaiter();
    }

    public void Dispose()
    {
        Stop();
        Configs._server.Dispose();
    }
}

public enum ConnectionState
{
    MethodSelectorReceived,
    SetMethodSent,
    RequestReceived,
    Connecting,
    ExchangingData,
}