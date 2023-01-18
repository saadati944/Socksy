using Socksy.Core.Common;
using System.Buffers;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Socksy.Core;

public sealed class Socks5Server : IDisposable
{
    private const int VER = 5;
    
    private readonly TcpServer _server;
    private readonly Action<Request>? _onClientConnected;

    private long _inCounter;
    private long _outCounter;
    
    public Socks5Server(IPEndPoint localEndPoint, Action<Request>? onClientConnected = null)
    {
        _server = new TcpServer(localEndPoint, OnConnectionEstablished);
        _onClientConnected = onClientConnected;
    }

    public bool IsListening => _server.IsListening;

    public long OutGoingBytes => _outCounter;
    public long InCommingBytes => _inCounter;

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
        _onClientConnected?.Invoke(request);
        var socket = request.CreateSocket();

        Log(reqNum, "Getting MethodSelectorDTO");
        var method = MethodSelectorDTO.GetFromSocket(socket);
        if (method is null || method.VER != 5 || !method.METHODS!.Contains((byte)AuthenticationMETHOD.NO_AUTHENTICATION_REQUIRED))
            throw new Exception("Bad request, client does not support NO AUTHENTICATION REQUIRED method");
        Log(reqNum, $"MethodSelectorDTO received. ver: {method.VER}, nmethods: {method.NMETHODS}, methods: '{string.Join(", ", method.METHODS!)}'");

        Log(reqNum, "Sending SetMethodDTO");
        var sm = SetMethodDTO.Create(VER, AuthenticationMETHOD.NO_AUTHENTICATION_REQUIRED);
        sm.Send(socket);
        Log(reqNum, $"SetMethodDTO sent. ver: {sm.VER}, method: {sm.METHOD}");

        Log(reqNum, "Getting RequestDTO");
        var req = RequestDTO.GetFromSocket(socket);
        Log(reqNum, $"RequestDTO received. VER: {req.VER}, CMD: {req.CMD}, RSV: {req.RSV}, ATYPE: {req.ATYPE}, DST_ADDR: {req.DST_ADDR}, DST_ADDR_STRING: {req.DST_ADDR_STRING}, DST_ADDR_IPADDRESS: {req.DST_ADDR_IPADDRESS}");

        if(req.CMD == RequestCMD.CONNECT)
        {
            IPEndPoint remote_endpoint = new IPEndPoint(
                req.ATYPE switch
                {
                    AddressTYPE.IPV4 or AddressTYPE.IPV6 => new IPAddress(req.DST_ADDR!),
                    _ => Dns.GetHostAddresses(req.DST_ADDR_STRING, AddressFamily.InterNetwork)[0],
                },
                req.DST_PORT);
            Log(reqNum, $"Connecting to remote endpoint by this information: ip address: {remote_endpoint.Address}, port: {remote_endpoint.Port}");

            TcpClient remoteClient = new TcpClient();
            await remoteClient.ConnectAsync(remote_endpoint);
            var remote = new SocketWrapper(remoteClient.Client);
            Log(reqNum, $"Connected: {remoteClient.Connected}");

            Log(reqNum, "Sending ReplyDTO");
            var rep = ReplyDTO.Create(
                VER,
                ReplyREP.Succeeded,
                _server.ListeningAddress.AddressFamily == AddressFamily.InterNetwork ? AddressTYPE.IPV4 : AddressTYPE.IPV6,
                _server.ListeningAddress.GetAddressBytes(),
                (ushort)_server.ListeningPort);
            rep.Send(socket);
            Log(reqNum, $"ReplyDTO received. VER: {rep.VER}, REP: {rep.REP}, RSV: {rep.RSV}, ATYPE: {rep.ATYPE}, BND_ADDR: {rep.BND_ADDR}, BND_ADDR_STRING: {rep.BND_ADDR_STRING}");

            Log(reqNum, "Exchanging data ...");
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

    private async Task ExchangeData(int reqNum, ISocket remote, ISocket client)
    {
        remote.SendTimeout = 2000;
        remote.ReceiveTimeout = 2000;
        client.SendTimeout = 2000;
        client.ReceiveTimeout = 2000;

        var t1 = MakeBridge(reqNum, remote, client, (i) => Interlocked.Add(ref _inCounter, i));
        var t2 = MakeBridge(reqNum, client, remote, (i) => Interlocked.Add(ref _outCounter, i));

        await Task.WhenAll(t1, t2);
        remote.Close();
        client.Close();
    }

    private static async Task MakeBridge(int reqNum, ISocket from, ISocket to, Action<int>? onAfterSendingData)
    {
        Log(reqNum, $"Make bridge from {from.RemoteEndPoint.Address}:{from.RemoteEndPoint.Port} to {to.RemoteEndPoint.Address}:{to.RemoteEndPoint.Port}");
        try
        {
            int fails = 2000;
            while (true)
            {
                if (!from.Connected || !to.Connected)
                    break;
                var avail = from.Available;
                if (avail == 0)
                {
                    if (from.Poll(3000, SelectMode.SelectRead) && fails-- <= 0)
                        break;
                    await Task.Delay(5);
                    continue;
                }
                fails = 20;
                var buf = ArrayPool<byte>.Shared.Rent(avail);
                from.Receive(new Span<byte>(buf, 0, avail));
                to.Send(new Span<byte>(buf, 0, avail));
                onAfterSendingData?.Invoke(avail);
            }
        }
        catch
        {
        }
        Log(reqNum, $"Bridge from {from.RemoteEndPoint.Address}:{from.RemoteEndPoint.Port} to {to.RemoteEndPoint.Address}:{to.RemoteEndPoint.Port} collapsed ...");
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

    private static void Log(int requestNumber, string message)
        => Console.WriteLine($"{requestNumber:D4} {message}");
}
