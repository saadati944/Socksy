namespace Socksy.Core.Dtos;

public class SetMethodDTO
{
    public byte VER { get; private set; }
    public byte METHOD { get; private set; }

    public static SetMethodDTO Create(byte ver, byte method)
    {
        return new SetMethodDTO
        {
            VER = ver,
            METHOD = method
        };
    }

    public void Send(Socket socket)
    {
        socket.Send(new byte[] { VER, METHOD });
    }
}
