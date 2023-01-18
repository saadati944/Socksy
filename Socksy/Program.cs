using Socksy.Core;
using System.Net;

Console.WriteLine("Socksy");
Socks5Server server = new Socks5Server(IPEndPoint.Parse("127.0.0.1:3050"));
server.Start();

Console.CancelKeyPress += (s, e) =>
{
    server.Stop();
};

_ = Task.Run(async () =>
{
    var t = new PeriodicTimer(TimeSpan.FromSeconds(1));
    while (await t.WaitForNextTickAsync())
        Console.Title = $"In: {server.InCommingBytes}, Out: {server.OutGoingBytes}";
});

await server;

Console.WriteLine("OK");
Console.WriteLine($"Total In: {server.InCommingBytes} bytes, Total Out: {server.OutGoingBytes} bytes");