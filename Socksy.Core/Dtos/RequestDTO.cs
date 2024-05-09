﻿using Socksy.Core.Common;

namespace Socksy.Core.Dtos;

internal sealed class RequestDTO
{
    public byte VER { get; private set; }
    public RequestCMD CMD { get; private set; }
    public byte RSV { get; private set; }
    public AddressTYPE ATYPE { get; private set; }
    public byte[]? DST_ADDR { get; private set; }
    public string? DST_ADDR_STRING { get; private set; }
    public IPAddress? DST_ADDR_IPADDRESS { get; private set; }
    public ushort DST_PORT { get; private set; }

    private RequestDTO()
    {
    }

    public static RequestDTO GetFromSocket(ISocket socket, Configs config)
    {
        var data = new byte[4];

        var received = socket.Receive(data);
        Helper.EnsureReceivedBytes(received, 4);
        var ver = data[0];
        var cmd = data[1];
        var rsv = data[2];
        var atype = data[3];

        data = Helper.RentByteArray(256);

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

        var DSTaddrString = atype == (int)AddressTYPE.DOMAINNAME
        ? System.Text.Encoding.ASCII.GetString(DSTaddr!)
        : string.Join(".", DSTaddr!);

        var DSTaddrIp = atype == (int)AddressTYPE.DOMAINNAME
            ? NetHelper.ResolveHost(DSTaddrString, config)
            : new IPAddress(DSTaddr!);

        return new RequestDTO
        {
            VER = ver,
            CMD = (RequestCMD)cmd,
            RSV = rsv,
            ATYPE = (AddressTYPE)atype,
            DST_ADDR = DSTaddr,
            DST_ADDR_STRING = DSTaddrString,
            DST_ADDR_IPADDRESS = DSTaddrIp,
            DST_PORT = DSTport
        };
    }
}
