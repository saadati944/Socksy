// See https://aka.ms/new-console-template for more information
using Socksy.Core;
using System.Net;

Console.WriteLine("Hello, World!");
Socks5Server server = new Socks5Server(IPEndPoint.Parse("127.0.0.1:3050"));
server.Start();

Console.CancelKeyPress += (s, e) =>
{
    server.Stop();
};

await server;

Console.WriteLine("OK");
Console.ReadLine();