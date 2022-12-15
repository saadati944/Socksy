namespace Socksy.Core.Dtos;

internal class MethodSelectorDTO
{
    public byte VER { get; private set; }
    public byte NMETHODS { get; private set; }
    public byte[]? METHODS { get; private set; }

    private MethodSelectorDTO()
    {
    }

    public static MethodSelectorDTO GetFromSocket(ISocket socket)
    {
        Span<byte> b = stackalloc byte[2];
        var received = socket.Receive(b);
        Helper.EnsureReceivedBytes(received, 2);

        var ms = new byte[b[1]];
        received = socket.Receive(ms);
        Helper.EnsureReceivedBytes(received, b[1]);

        return new MethodSelectorDTO
        {
            VER = b[0],
            NMETHODS = b[1],
            METHODS = ms
        };
    }
}
