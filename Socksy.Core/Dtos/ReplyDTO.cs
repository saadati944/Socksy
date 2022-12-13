using Socksy.Core.Common;

namespace Socksy.Core.Dtos;

public class ReplyDTO
{
    public byte VER { get; private set; }
    public ReplyREP REP { get; private set; }
    public byte RSV => 0;
    public AddressTYPE ATYPE { get; private set; }
    public byte[] BND_ADDR { get; private set; }
    public string BND_ADDR_STRING => ATYPE == AddressTYPE.DOMAINNAME
        ? System.Text.Encoding.ASCII.GetString(BND_ADDR)
        : string.Join(", ", BND_ADDR);
    public ushort BND_PORT { get; private set; }

    public void Send(Socket socket)
    {
        var bytes = new byte[4 + BND_ADDR.Length + 2];
        bytes[0] = VER;
        bytes[1] = (byte)REP;
        bytes[2] = RSV;
        bytes[3] = (byte)ATYPE;
        Array.Copy(BND_ADDR, 0, bytes, 4, BND_ADDR.Length);
        bytes[^2] = (byte)(BND_PORT / 256);
        bytes[^1] = (byte)(BND_PORT % 256);
        socket.Send(bytes);
    }

    public static ReplyDTO Create(byte ver, ReplyREP rep, AddressTYPE atype, byte[] bnd_addr, ushort port)
    {
        return new ReplyDTO
        {
            VER = ver,
            REP = rep,
            ATYPE = atype,
            BND_ADDR = bnd_addr,
            BND_PORT = port
        };
    }
}
