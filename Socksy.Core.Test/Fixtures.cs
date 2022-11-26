namespace Socksy.Core.Test;

internal static class Fixtures
{
    public static IPEndPoint GetLocalendpointWithTemplatePortNumber()
    {
        return new IPEndPoint(IPAddress.Loopback, GetNextFreePort());
    }

    private static int GetNextFreePort()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }
}
