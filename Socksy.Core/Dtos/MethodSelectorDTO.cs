namespace Socksy.Core.Dtos;

public struct MethodSelectorDTO
{
    public byte VER { get; private set; }
    public byte NMETHODS { get; private set; }
    public byte[] METHODS { get; private set; }

    public static MethodSelectorDTO GetFromSocket(Socket socket)
    {
        Span<byte> b = stackalloc byte[2];
        var received = socket.Receive(b);
        if (received != 2)
            throw new Exception("Bad request!");
        Span<byte> ms = stackalloc byte[b[1]];
        received = socket.Receive(ms);
        if (received != b[1])
            throw new Exception("Bad request!");

        return new MethodSelectorDTO
        {
            VER = b[0],
            NMETHODS = b[1],
            METHODS = ms.ToArray()
        };
    }
}
