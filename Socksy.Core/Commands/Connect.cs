using System.Buffers;
using Socksy.Core.Common;

namespace Socksy.Core.Commands;

internal static class ConnectCommand
{
    public static async Task ExecuteConnect(int reqNum, ISocket socket, RequestDTO req)
    {
        if (AddressIsInBlackList(req.DST_ADDR_STRING))
        {
            Configs.Log(reqNum, "Requested address was in black list");
            var blockedRep = ReplyDTO.Create(
                Configs.VER,
                ReplyREP.Connection_not_allowed_by_ruleset,
                Configs._server.ListeningAddress.AddressFamily == AddressFamily.InterNetwork ? AddressTYPE.IPV4 : AddressTYPE.IPV6,
                Configs._server.ListeningAddress.GetAddressBytes(),
                (ushort)Configs._server.ListeningPort);
            blockedRep.Send(socket);
            Configs.Log(reqNum, $"Not-Allowed Reply Sent. VER: {blockedRep.VER}, REP: {blockedRep.REP}, RSV: {blockedRep.RSV}, ATYPE: {blockedRep.ATYPE}, BND_ADDR: {blockedRep.BND_ADDR}, BND_ADDR_STRING: {blockedRep.BND_ADDR_STRING}");
            return;
        }

        IPEndPoint remote_endpoint = new IPEndPoint(
            req.ATYPE switch
            {
                AddressTYPE.IPV4 or AddressTYPE.IPV6 => new IPAddress(req.DST_ADDR!),
                _ => Dns.GetHostAddresses(req.DST_ADDR_STRING, AddressFamily.InterNetwork)[0],
            },
            req.DST_PORT);
        Configs.Log(reqNum, $"Connecting to remote endpoint. IP address: {remote_endpoint.Address}, port: {remote_endpoint.Port}");
        Configs._activeConnections[reqNum] = ConnectionState.Connecting;

        TcpClient remoteClient = new TcpClient();
        await remoteClient.ConnectAsync(remote_endpoint);
        var remote = new SocketWrapper(remoteClient.Client);
        Configs.Log(reqNum, $"Connected: {remoteClient.Connected}");

        Configs.Log(reqNum, "Sending Reply");
        var rep = ReplyDTO.Create(
            Configs.VER,
            ReplyREP.Succeeded,
            Configs._server.ListeningAddress.AddressFamily == AddressFamily.InterNetwork ? AddressTYPE.IPV4 : AddressTYPE.IPV6,
            Configs._server.ListeningAddress.GetAddressBytes(),
            (ushort)Configs._server.ListeningPort);
        rep.Send(socket);
        Configs.Log(reqNum, $"Reply Sent. VER: {rep.VER}, REP: {rep.REP}, RSV: {rep.RSV}, ATYPE: {rep.ATYPE}, BND_ADDR: {rep.BND_ADDR}, BND_ADDR_STRING: {rep.BND_ADDR_STRING}");

        Configs.Log(reqNum, "Exchanging data ...");
        Configs._activeConnections[reqNum] = ConnectionState.ExchangingData;
        await ExchangeData(reqNum, remote, socket);
    }
    
    private static bool AddressIsInBlackList(string destinationAddress)
    {
        if (Configs._blackListRegexes is null || Configs._blackListRegexes.Length == 0)
            return false;

        for (int i = 0; i < Configs._blackListRegexes.Length; i++)
            if (Configs._blackListRegexes[i].IsMatch(destinationAddress))
                return true;

        return false;
    }

    private static async Task ExchangeData(int reqNum, ISocket remote, ISocket client)
    {
        remote.SendTimeout = 20000;
        remote.ReceiveTimeout = 20000;
        client.SendTimeout = 20000;
        client.ReceiveTimeout = 20000;

        var t1 = MakeBridge(reqNum, remote, client, (i) => Interlocked.Add(ref Configs._inCounter, i));
        var t2 = MakeBridge(reqNum, client, remote, (i) => Interlocked.Add(ref Configs._outCounter, i));

        await Task.WhenAny(t1, t2);
        remote.Close();
        client.Close();
    }

    private static async Task MakeBridge(int reqNum, ISocket from, ISocket to, Action<int>? onAfterSendingData)
    {
        Configs.Log(reqNum, $"Making bridge from {from.RemoteEndPoint.Address}:{from.RemoteEndPoint.Port} to {to.RemoteEndPoint.Address}:{to.RemoteEndPoint.Port}");

        try
        {
            int fails = 200;
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
                fails = 200;
                var buf = ArrayPool<byte>.Shared.Rent(avail);
                from.Receive(new Span<byte>(buf, 0, avail));
                to.Send(new Span<byte>(buf, 0, avail));
                onAfterSendingData?.Invoke(avail);
            }
        }
        catch
        {
        }
        Configs.Log(reqNum, $"Bridge from {from.RemoteEndPoint.Address}:{from.RemoteEndPoint.Port} to {to.RemoteEndPoint.Address}:{to.RemoteEndPoint.Port} collapsed ...");
    }
}