using System.Text.Json;
using Socksy.Core;
using Socksy.Core.Common;

bool debugMode = false;
#if DEBUG
debugMode = true;
#endif

bool verboseLogging = false;
if (args.Contains("-v"))
{
    verboseLogging = true;
}

string? optinosJson = null;
if (File.Exists("./config.json"))
{
    optinosJson = File.ReadAllText("./config.json");
}

var options = JsonSerializer.Deserialize<ServerOptions>(optinosJson ?? string.Empty);
if(options is null ) options = ServerOptions.Default;

Socks5Server server = new Socks5Server(
    logFunction: debugMode ? LogFunction : null,
    options: options!
);

server.Start();

Console.WriteLine($"Listening on {options.EndPoint}");
if (verboseLogging || debugMode)
    Console.Title = $"Listening on {options.EndPoint}";

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

void LogFunction(int n, string s)
{
    Console.WriteLine(n < 0
        ? s
        : $"{n:D6} {s}"
    );
}