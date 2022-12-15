namespace Socksy.Core.Dtos;

internal class SetMethodDTO
{
    public byte VER { get; private set; }
    public byte METHOD { get; private set; }

    private SetMethodDTO()
    {
    }

    public static SetMethodDTO Create(byte ver, byte method)
    {
        return new SetMethodDTO
        {
            VER = ver,
            METHOD = method
        };
    }

    public void Send(ISocket socket)
    {
        var sent = socket.Send(stackalloc byte[] { VER, METHOD });
        Helper.EnsureSentBytes(sent, 2);
    }
}
