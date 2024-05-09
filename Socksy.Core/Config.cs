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
    public IPEndPoint EndPoint { get; set; } = IPEndPoint.Parse(ServerOptions.Default.EndPoint);
    public int SocketTimeOut = 20000;
    public int DisconnectAFterNPolls = 2000;

    public long InCounter;
    public long OutCounter;

    public Configs(ServerOptions? options)
    {
        Options = options ?? ServerOptions.Default;
        InitializeOptions();
    }

    private void InitializeOptions()
    {
        IPEndPoint endpoint;
        if (!string.IsNullOrWhiteSpace(Options.EndPoint) && IPEndPoint.TryParse(Options.EndPoint, out endpoint!))
        {
            EndPoint = endpoint;
        }

        if(Options.SocketTimeOutMS > 0)
        {
            SocketTimeOut = Options.SocketTimeOutMS;
        }

        if(Options.DisconnectAFterNPolls > 0)
        {
            DisconnectAFterNPolls = Options.DisconnectAFterNPolls;
        }

        if (Options.BlackList is not null && Options.BlackList.Length > 0)
        {
            Log(-1, $"Initializing black list with {Options.BlackList.Length} items");
            BlackListRegexes = new Regex[Options.BlackList.Length];
            for (int i = 0; i < Options.BlackList.Length; i++)
                BlackListRegexes[i] = new Regex(WildCardToRegex(Options.BlackList[i]), RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
        }

        if (Options.WhiteList is not null && Options.WhiteList.Length > 0)
        {
            if(BlackListRegexes.Length > 0)
            {
                throw new Exception("Cannot use both blacklist and whitelist at the same time. Provide one instead");
            }

            Log(-1, $"Initializing while list with {Options.WhiteList.Length} items");
            WhiteListRegexes = new Regex[Options.WhiteList.Length];
            for (int i = 0; i < Options.WhiteList.Length; i++)
                WhiteListRegexes[i] = new Regex(WildCardToRegex(Options.WhiteList[i]), RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
        }
    }

    private static string WildCardToRegex(string value) {
        return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$"; 
    }

    [Conditional("DEBUG")]
    public void Log(int requestNumber, string message)
        => LogAction?.Invoke(requestNumber, message);
}