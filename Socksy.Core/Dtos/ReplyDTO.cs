using Socksy.Core.Common;

namespace Socksy.Core.Dtos;

internal sealed class ReplyDTO
{
    public byte VER { get; private set; }
    public ReplyREP REP { get; private set; }
    public byte RSV => 0;
    public AddressTYPE ATYPE { get; private set; }
    public byte[]? BND_ADDR { get; private set; }
    public string BND_ADDR_STRING => ATYPE == AddressTYPE.DOMAINNAME
        ? System.Text.Encoding.ASCII.GetString(BND_ADDR!)
        : string.Join(", ", BND_ADDR!);

    public ushort BND_PORT { get; private set; }

    private ReplyDTO()
    {
    }

    public void Send(ISocket socket)
    {
        var len = 4 + BND_ADDR!.Length + 2;
        var bytes = Helper.RentByteArray(len);

        bytes[0] = VER;
        bytes[1] = (byte)REP;
        bytes[2] = RSV;
        bytes[3] = (byte)ATYPE;
        Array.Copy(BND_ADDR, 0, bytes, 4, BND_ADDR.Length);
        bytes[len-2] = (byte)(BND_PORT / 256);
        bytes[len-1] = (byte)(BND_PORT % 256);

        int sent = socket.Send(bytes, 0, len);
        Helper.EnsureSentBytes(sent, len);

        Helper.ReturnByteArray(bytes);
    }

    public static ReplyDTO Create(byte ver, ReplyREP rep, AddressTYPE atype, byte[] bnd_addr, ushort port)
    {
        if (atype == AddressTYPE.DOMAINNAME)
            throw new ArgumentException("The Address type DOMAINNAME is not supported");

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
