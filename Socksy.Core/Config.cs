using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Socksy.Core.Common;

namespace Socksy.Core;

internal class Configs
{
    public const int VER = 5;

    public TcpServer Server { get; set; } = null!;
    public ConcurrentDictionary<int, ConnectionState> ActiveConnections { get; } = new ConcurrentDictionary<int, ConnectionState>();
    public Action<Request>? OnClientConnected { get; set; }
    public Action<int, string>? LogAction { get; set; }

    public ServerOptions Options { get; init; }
    public Regex[] BlackListRegexes { get; set; } = [];
    public Regex[] WhiteListRegexes { get; set; } = [];
    public Tuple<Regex, IPAddress>[] DnsMap { get; set; } = [];
    public int BandWidthBytesPerSecond;
    public BandWidthLimiter? BandWidthLimiter { get; private set; }
    public IPEndPoint EndPoint { get; set; } = IPEndPoint.Parse(ServerOptions.Default.EndPoint);
    public int SocketTimeOut;
    public int DisconnectAFterNPolls;

    public long InCounter;
    public long OutCounter;

    public Configs(ServerOptions? options)
    {
        Options = options ?? ServerOptions.Default;
        InitializeOptions();
    }

    private void InitializeOptions()
    {
        EndPoint = !string.IsNullOrWhiteSpace(Options.EndPoint) && IPEndPoint.TryParse(Options.EndPoint, out IPEndPoint? endpoint)
            ? endpoint!
            : IPEndPoint.Parse(ServerOptions.Default.EndPoint);

        SocketTimeOut = Options.SocketTimeOutMS > 0
            ? Options.SocketTimeOutMS
            : ServerOptions.Default.SocketTimeOutMS;

        DisconnectAFterNPolls = Options.DisconnectAFterNPolls > 0
            ? Options.DisconnectAFterNPolls
            : ServerOptions.Default.DisconnectAFterNPolls;

        if (Options.BlackList is not null && Options.BlackList.Length > 0)
        {
            Log(-1, $"Initializing black list with {Options.BlackList.Length} items");
            BlackListRegexes = new Regex[Options.BlackList.Length];
            for (int i = 0; i < Options.BlackList.Length; i++)
            {
                BlackListRegexes[i] = new Regex(WildCardToRegex(Options.BlackList[i]), RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
            }
        }

        if (Options.WhiteList is not null && Options.WhiteList.Length > 0)
        {
            if (BlackListRegexes.Length > 0)
                throw new Exception("Cannot use both blacklist and whitelist at the same time. Provide one instead");

            Log(-1, $"Initializing while list with {Options.WhiteList.Length} items");
            WhiteListRegexes = new Regex[Options.WhiteList.Length];
            for (int i = 0; i < Options.WhiteList.Length; i++)
            {
                WhiteListRegexes[i] = new Regex(WildCardToRegex(Options.WhiteList[i]), RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
            }
        }

        if (Options.DnsMap is not null && Options.DnsMap.Count > 0)
        {
            DnsMap = new Tuple<Regex, IPAddress>[Options.DnsMap.Count];
            var index = 0;
            foreach (var mapping in Options.DnsMap)
            {
                DnsMap[index++] = new Tuple<Regex, IPAddress>(
                    new Regex(WildCardToRegex(mapping.Key), RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200)),
                    IPAddress.Parse(mapping.Value));
            }
        }

        if (!string.IsNullOrWhiteSpace(Options.BandWidth) && Options.BandWidth.Trim().Length > 2)
        {
            var bandWidth = Options.BandWidth.Trim().ToLower();
            var unitString = bandWidth.Substring(bandWidth.Length - 2, 2);
            var valueString = bandWidth.Substring(0, bandWidth.Length - 2).Trim();
            var value = decimal.Parse(valueString);
            BandWidthBytesPerSecond = unitString switch
            {
                "kb" => (int)value * 1000,
                "mb" => (int)value * 1000 * 1000,
                "gb" => (int)value * 1000 * 1000 * 1000,
                _ => throw new Exception("possible units for bandwith are: kb, mb, gb")
            };

            if(value <= 0)
                throw new Exception("bandwidth value should be a positive number");

            BandWidthLimiter = new BandWidthLimiter(BandWidthBytesPerSecond);
        }
    }

    public bool AddressIsInWhiteList(string destinationAddress)
    {
        if (WhiteListRegexes.Length == 0) return true;

        for (int i = 0; i < WhiteListRegexes.Length; i++)
            if (WhiteListRegexes[i].IsMatch(destinationAddress))
                return true;

        return false;
    }

    public bool AddressIsInBlackList(string destinationAddress)
    {
        if (BlackListRegexes.Length == 0) return false;

        for (int i = 0; i < BlackListRegexes.Length; i++)
            if (BlackListRegexes[i].IsMatch(destinationAddress))
                return true;

        return false;
    }

    private static string WildCardToRegex(string value)
    {
        return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";
    }

    [Conditional("DEBUG")]
    public void Log(int requestNumber, string message)
        => LogAction?.Invoke(requestNumber, message);

    internal async ValueTask<int> GetAllowedBytesToReceive(int avail)
    {
        if(BandWidthLimiter is null) return avail;
        return await BandWidthLimiter.GetAllowedBytes(avail);
    }
}