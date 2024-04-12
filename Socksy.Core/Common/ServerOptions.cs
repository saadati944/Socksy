namespace Socksy.Core.Common;

public sealed class ServerOptions
{
    public string EndPoint { get; init; } = "0.0.0.0:3050";
    public int SocketTimeOutMS { get; init; } = 20000;
    public string[]? BlackList { get; init; }
    public string[]? WhiteList { get; init; }
    public string[][]? DnsMap { get; init; }

    public static ServerOptions Default { get; }
        = new ServerOptions
        {
        };
}