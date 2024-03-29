using Socksy.Core;
using System.Net;

bool debugMode = false;
#if DEBUG
debugMode = true;
#endif

bool verboseLogging = false;
if (args.Contains("-v"))
{
    verboseLogging = true;
}

string[]? blacklist = null;
if (File.Exists("./blacklist.txt"))
{
    blacklist = File.ReadAllLines("./blacklist.txt").Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
}
Socks5Server server = new Socks5Server(IPEndPoint.Parse("127.0.0.1:3050"),
    logFunction: debugMode ? (n, s) => Console.WriteLine($"{n:D6} {s}") : null,
    options: new Socksy.Core.Common.ServerOptions
    {
        BlockedAddresses = blacklist
    });
server.Start();

Console.WriteLine("Listening on 127.0.0.1:3050");
if (verboseLogging || debugMode)
    Console.Title = "Listening on 127.0.0.1:3050";

Console.CancelKeyPress += (s, e) =>
{
    server.Stop();
};

if (verboseLogging || debugMode)
    _ = Task.Run(async () =>
    {
        var t = new PeriodicTimer(TimeSpan.FromSeconds(1));
        if (debugMode)
        {
            while (await t.WaitForNextTickAsync())
            {
                Console.Title = $"Connections: {server.ActiveConnections.Count}, In: {server.InCommingBytes}, Out: {server.OutGoingBytes}";
            }
        }
        else
        {
            while (await t.WaitForNextTickAsync())
            {
                var output = string.Join('\n', server.ActiveConnections.Select(c => $"{c.Key:D6} {c.Value}"));
                Console.Clear();
                Console.WriteLine($"Total connections: {server.ActiveConnections.Count}");
                Console.WriteLine($"Total in: {server.InCommingBytes}");
                Console.WriteLine($"Total out: {server.OutGoingBytes}");
                Console.WriteLine(output);
            }
        }
    });

await server;

Console.WriteLine("OK");
Console.WriteLine($"Total In: {server.InCommingBytes} bytes, Total Out: {server.OutGoingBytes} bytes");