using Socksy.Core;
using System.Net;

bool debugMode = true;

Console.WriteLine("Listening on 127.0.0.1:3050");
Socks5Server server = new Socks5Server(IPEndPoint.Parse("127.0.0.1:3050"),
    logFunction: debugMode ? (n, s) => Console.WriteLine($"{n:D4} {s}") : null);

server.Start();

Console.CancelKeyPress += (s, e) =>
{
    server.Stop();
};

_ = Task.Run(async () =>
{
    var t = new PeriodicTimer(TimeSpan.FromSeconds(1));
    if (debugMode)
    {
        while (await t.WaitForNextTickAsync())
        {
            Console.Title = $"Connections: {server.ActiveConnections}, In: {server.InCommingBytes}, Out: {server.OutGoingBytes}";
        }
    }
    else
    {
        while (await t.WaitForNextTickAsync())
        {
            var output = string.Join('\n', server.ActiveConnections.Select(c => $"{c.Key:D4} {c.Value}"));
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