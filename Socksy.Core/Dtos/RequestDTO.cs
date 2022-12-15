using Socksy.Core.Common;

namespace Socksy.Core.Dtos;

internal class RequestDTO
{
    public byte VER { get; private set; }
    public RequestCMD CMD { get; private set; }
    public byte RSV { get; private set; }
    public AddressTYPE ATYPE { get; private set; }
    public byte[]? DST_ADDR { get; private set; }
    public string DST_ADDR_STRING => ATYPE == AddressTYPE.DOMAINNAME
        ? System.Text.Encoding.ASCII.GetString(DST_ADDR!)
        : string.Join(", ", DST_ADDR!);

    public IPAddress? DST_ADDR_IPADDRESS => ATYPE == AddressTYPE.DOMAINNAME
        ? Dns.GetHostAddresses(DST_ADDR_STRING).OrderByDescending(i => i.AddressFamily == AddressFamily.InterNetwork ? 1 : 0).FirstOrDefault()
        : new IPAddress(DST_ADDR!);

    public ushort DST_PORT { get; private set; }

    private RequestDTO()
    {
    }

    public static RequestDTO GetFromSocket(ISocket socket)
    {
        var data = Helper.RentByteArray(256);

        var received = socket.Receive(data, 0, 4);
        Helper.EnsureReceivedBytes(received, 4);

        var ver = data[0];
        var cmd = data[1];
        var rsv = data[2];
        var atype = data[3];

        var dstAddrLen = 0;
        switch (atype)
        {
            case 3:
                received = socket.Receive(data, 0, 1);
                Helper.EnsureReceivedBytes(received, 1);
                dstAddrLen = data[0];
                break;
            case 1:
                dstAddrLen = 4;
                break;
            case 4:
                dstAddrLen = 16;
                break;
        }

        received = socket.Receive(data, 0, dstAddrLen);
        Helper.EnsureReceivedBytes(received, dstAddrLen);
        var DSTaddr = new byte[dstAddrLen];
        Array.Copy(data, 0, DSTaddr, 0, dstAddrLen);

        received = socket.Receive(data, 0, 2);
        Helper.EnsureReceivedBytes(received, 2);
        ushort DSTport = Helper.GetPort(data);

        Helper.ReturnByteArray(data);

        return new RequestDTO
        {
            VER = ver,
            CMD = (RequestCMD)cmd,
            RSV = rsv,
            ATYPE = (AddressTYPE)atype,
            DST_ADDR = DSTaddr,
            DST_PORT = DSTport
        };
    }
}
