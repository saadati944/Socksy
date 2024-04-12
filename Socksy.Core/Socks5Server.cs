using Socksy.Core.Commands;
using Socksy.Core.Common;
using System.Runtime.CompilerServices;

namespace Socksy.Core;

public sealed class Socks5Server : IDisposable
{
    private readonly Configs configs;
    private readonly ConnectCommand connectCommand;


    public Socks5Server(Action<Request>? onClientConnected = null, Action<int, string>? logFunction = null, ServerOptions? options = null)
    {
        configs = new Configs(options)
        {
            OnClientConnected = onClientConnected,
            LogAction = logFunction
        };
        configs.Server = new TcpServer(configs.EndPoint, OnConnectionEstablished);

        connectCommand = new ConnectCommand(configs);
    }

    public bool IsListening => configs.Server.IsListening;

    public long OutGoingBytes => configs.OutCounter;
    public long InCommingBytes => configs.InCounter;
    public IReadOnlyDictionary<int, ConnectionState> ActiveConnections => configs.ActiveConnections;

    public void Start()
    {
        configs.Server.Start();
    }

    public void Stop()
    {
        configs.Server.Stop();
    }

    private async Task OnConnectionEstablished(int reqNum, Request request)
    {
        try
        {
            configs.OnClientConnected?.Invoke(request);
            var socket = request.CreateSocket();

            GetMethodSelector(reqNum, socket);
            SendSetMethod(reqNum, socket);
            RequestDTO req = GetRequest(reqNum, socket);

            if (req.CMD == RequestCMD.CONNECT)
            {
                await connectCommand.ExecuteConnect(reqNum, socket, req);
            }
            // else if(req.CMD == ...
            else
            {
                ReplyDTO.Create(
                    Configs.VER,
                    ReplyREP.Command_not_supported,
                    configs.Server.ListeningAddress.AddressFamily == AddressFamily.InterNetwork ? AddressTYPE.IPV4 : AddressTYPE.IPV6,
                    configs.Server.ListeningAddress.GetAddressBytes(),
                    (ushort)configs.Server.ListeningPort);
                throw new Exception("Request command is not supported");
            }
        }
        finally
        {
            _ = configs.ActiveConnections.TryRemove(reqNum, out var _);
        }
    }

    private RequestDTO GetRequest(int reqNum, ISocket socket)
    {
        configs.Log(reqNum, "Waiting for RequestDTO ...");
        var req = RequestDTO.GetFromSocket(socket);
        configs.Log(reqNum, $"RequestDTO received. VER: {req.VER}, CMD: {req.CMD}, RSV: {req.RSV}, ATYPE: {req.ATYPE}, DST_ADDR: {req.DST_ADDR}, DST_ADDR_STRING: {req.DST_ADDR_STRING}, DST_ADDR_IPADDRESS: {req.DST_ADDR_IPADDRESS}");
        configs.ActiveConnections[reqNum] = ConnectionState.RequestReceived;
        return req;
    }

    private void SendSetMethod(int reqNum, ISocket socket)
    {
        configs.Log(reqNum, "Sending SetMethodDTO");
        var sm = SetMethodDTO.Create(Configs.VER, AuthenticationMETHOD.NO_AUTHENTICATION_REQUIRED);
        sm.Send(socket);
        configs.Log(reqNum, $"SetMethod sent. ver: {sm.VER}, method: {sm.METHOD}");
        configs.ActiveConnections[reqNum] = ConnectionState.SetMethodSent;
    }

    private void GetMethodSelector(int reqNum, ISocket socket)
    {
        configs.Log(reqNum, "Waiting for MethodSelector ...");
        var method = MethodSelectorDTO.GetFromSocket(socket);
        if (method is null || method.VER != 5 || !method.METHODS!.Contains((byte)AuthenticationMETHOD.NO_AUTHENTICATION_REQUIRED))
            throw new Exception("Bad request, client does not support 'NO AUTHENTICATION REQUIRED' method");
        configs.Log(reqNum, $"MethodSelector received. ver: {method.VER}, nmethods: {method.NMETHODS}, methods: '{string.Join(", ", method.METHODS!)}'");
        configs.ActiveConnections[reqNum] = ConnectionState.MethodSelectorReceived;
    }

    public TaskAwaiter GetAwaiter()
    {
        return configs.Server.GetAwaiter();
    }

    public void Dispose()
    {
        Stop();
        configs.Server.Dispose();
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