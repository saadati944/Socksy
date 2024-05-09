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

    internal static IPAddress? ResolveHost(string hostName, Configs config)
    {
        if(config.DnsMap.Length > 0)
        {
            foreach (var mapping in config.DnsMap)
            {
                if(mapping.Item1.IsMatch(hostName))
                    return mapping.Item2;
            }
        }

        return Dns.GetHostAddresses(hostName).OrderBy(i => i.AddressFamily == AddressFamily.InterNetwork ? 0 : 1).FirstOrDefault();
    }
}
