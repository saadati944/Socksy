using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Socksy.Core;

internal static class Configs
{
    public const int VER = 5;

    public static TcpServer _server;
    public static Action<Request>? _onClientConnected;
    public static Action<int, string>? _log;
    public static Regex[]? _blackListRegexes = null;

    public static ConcurrentDictionary<int, ConnectionState> _activeConnections;
    public static long _inCounter;
    public static long _outCounter;
    
    [Conditional("DEBUG")]
    public static void Log(int requestNumber, string message)
        => _log?.Invoke(requestNumber, message);
}