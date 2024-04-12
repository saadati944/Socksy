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
    public Regex[]? BlackListRegexes { get; set; } = null;
    public IPEndPoint EndPoint { get; set; } = IPEndPoint.Parse(ServerOptions.Default.EndPoint);
    public int SocketTimeOut = 20000;

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

        if (Options.BlackList is not null && Options.BlackList.Length > 0)
        {
            Log(-1, $"Initializing black list with {Options.BlackList.Length} items");
            BlackListRegexes = new Regex[Options.BlackList.Length];
            for (int i = 0; i < Options.BlackList.Length; i++)
                BlackListRegexes[i] = new Regex(Options.BlackList[i], RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(200));
        }
    }

    [Conditional("DEBUG")]
    public void Log(int requestNumber, string message)
        => LogAction?.Invoke(requestNumber, message);
}