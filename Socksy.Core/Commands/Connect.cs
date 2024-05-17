using System.Buffers;
using Socksy.Core.Common;

namespace Socksy.Core.Commands;

internal class ConnectCommand
{
    private readonly Configs configs;

    public ConnectCommand(Configs configs)
    {
        this.configs = configs;
    }

    public async Task ExecuteConnect(int reqNum, ISocket socket, RequestDTO req)
    {
        if (configs.AddressIsInBlackList(req.DST_ADDR_STRING) || !configs.AddressIsInWhiteList(req.DST_ADDR_STRING))
        {
            configs.Log(reqNum, "Requested address was in black list");
            var blockedRep = ReplyDTO.Create(
                Configs.VER,
                ReplyREP.Connection_not_allowed_by_ruleset,
                configs.Server.ListeningAddress.AddressFamily == AddressFamily.InterNetwork ? AddressTYPE.IPV4 : AddressTYPE.IPV6,
                configs.Server.ListeningAddress.GetAddressBytes(),
                (ushort)configs.Server.ListeningPort);
            blockedRep.Send(socket);
            configs.Log(reqNum, $"Not-Allowed Reply Sent. VER: {blockedRep.VER}, REP: {blockedRep.REP}, RSV: {blockedRep.RSV}, ATYPE: {blockedRep.ATYPE}, BND_ADDR: {blockedRep.BND_ADDR}, BND_ADDR_STRING: {blockedRep.BND_ADDR_STRING}");
            return;
        }

        var addr = req.DST_ADDR_IPADDRESS;
        if(addr is null)
        {
            configs.Log(reqNum, $"could not resolve host: {req.DST_ADDR_STRING}");
            return;
        }

        IPEndPoint remote_endpoint = new IPEndPoint(addr, req.DST_PORT);
        configs.Log(reqNum, $"Connecting to remote endpoint. IP address: {remote_endpoint.Address}, port: {remote_endpoint.Port}");
        configs.ActiveConnections[reqNum] = ConnectionState.Connecting;

        TcpClient remoteClient = new TcpClient();
        await remoteClient.ConnectAsync(remote_endpoint);
        var remote = new SocketWrapper(remoteClient.Client);
        configs.Log(reqNum, $"Connected: {remoteClient.Connected}");

        configs.Log(reqNum, "Sending Reply");
        var rep = ReplyDTO.Create(
            Configs.VER,
            ReplyREP.Succeeded,
            configs.Server.ListeningAddress.AddressFamily == AddressFamily.InterNetwork ? AddressTYPE.IPV4 : AddressTYPE.IPV6,
            configs.Server.ListeningAddress.GetAddressBytes(),
            (ushort)configs.Server.ListeningPort);
        rep.Send(socket);
        configs.Log(reqNum, $"Reply Sent. VER: {rep.VER}, REP: {rep.REP}, RSV: {rep.RSV}, ATYPE: {rep.ATYPE}, BND_ADDR: {rep.BND_ADDR}, BND_ADDR_STRING: {rep.BND_ADDR_STRING}");

        configs.Log(reqNum, "Exchanging data ...");
        configs.ActiveConnections[reqNum] = ConnectionState.ExchangingData;
        await ExchangeData(reqNum, remote, socket);
    }

    private async Task ExchangeData(int reqNum, ISocket remote, ISocket client)
    {
        remote.SendTimeout = configs.SocketTimeOut;
        remote.ReceiveTimeout = configs.SocketTimeOut;
        client.SendTimeout = configs.SocketTimeOut;
        client.ReceiveTimeout = configs.SocketTimeOut;

        var t2 = MakeBridge(reqNum, client, remote, (i) => Interlocked.Add(ref configs.OutCounter, i));
        var t1 = MakeBridge(reqNum, remote, client, (i) => Interlocked.Add(ref configs.InCounter, i));

        await Task.WhenAny(t1, t2);
        remote.Close();
        client.Close();
    }

    private async Task MakeBridge(int reqNum, ISocket from, ISocket to, Action<int>? onAfterSendingData)
    {
        configs.Log(reqNum, $"Making bridge from {from.RemoteEndPoint.Address}:{from.RemoteEndPoint.Port} to {to.RemoteEndPoint.Address}:{to.RemoteEndPoint.Port}");

        try
        {
            int fails = configs.DisconnectAFterNPolls;
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
                fails = configs.DisconnectAFterNPolls;

                avail = await configs.GetAllowedBytesToReceive(avail);

                var buf = ArrayPool<byte>.Shared.Rent(avail);
                from.Receive(new Span<byte>(buf, 0, avail));
                to.Send(new Span<byte>(buf, 0, avail));
                onAfterSendingData?.Invoke(avail);
            }
        }
        catch
        {
        }
        configs.Log(reqNum, $"Bridge from {from.RemoteEndPoint.Address}:{from.RemoteEndPoint.Port} to {to.RemoteEndPoint.Address}:{to.RemoteEndPoint.Port} collapsed ...");
    }
}