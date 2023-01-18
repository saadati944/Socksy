using Socksy.Core.Common;

namespace Socksy.Core.Dtos;

internal class SetMethodDTO
{
    public byte VER { get; private set; }
    public AuthenticationMETHOD METHOD { get; private set; }

    private SetMethodDTO()
    {
    }

    public static SetMethodDTO Create(byte ver, AuthenticationMETHOD method)
    {
        return new SetMethodDTO
        {
            VER = ver,
            METHOD = method
        };
    }

    public void Send(ISocket socket)
    {
        var sent = socket.Send(stackalloc byte[] { VER, (byte)METHOD });
        Helper.EnsureSentBytes(sent, 2);
    }
}
