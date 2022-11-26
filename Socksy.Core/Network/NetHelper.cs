namespace Socksy.Core.Network;

internal static class NetHelper
{
    internal static async Task<TcpClient> CreateTcpConnectionTo(IPEndPoint endPoint)
    {
        var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(endPoint);
        return tcpClient;
    }

    internal static async Task<TcpClient> CreateTcpConnectionTo(IPEndPoint endPoint, int timeOut)
    {
        using var ctSource = new CancellationTokenSource();
        ctSource.CancelAfter(TimeSpan.FromMilliseconds(timeOut));
        var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(endPoint, ctSource.Token);
        return tcpClient;
    }

    internal static IPAddress? ResolveHost(string hostName)
    {
        var res = Dns.GetHostEntry(hostName);
        return res.AddressList.FirstOrDefault();
    }

    internal static IPAddress? ResolveHost(string hostName, AddressFamily addressFamily)
    {
        var res = Dns.GetHostEntry(hostName, addressFamily);
        return res.AddressList.FirstOrDefault();
    }
}
