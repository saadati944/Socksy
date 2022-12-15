using System.Text;

namespace Socksy.Core.Test.Dtos;

public class RequestDTOTests
{
    [Fact]
    public void GetFromSocket_WithIPv4Address_CreatesNewInstanceFromSocket()
    {
        // Arrange
        var ip = new byte[] { 192, 186, 201, 102 };
        var socket = new SocketMock(new List<byte>
        {
            5, // ver
            (byte)RequestCMD.CONNECT, // cmd
            0, // rsv
            (byte)AddressTYPE.IPV4, // atype
        });
        socket.DataToReceive.AddRange(ip); // dstaddr
        socket.DataToReceive.Add(2); // dstport
        socket.DataToReceive.Add(1); // dstport


        // Act
        var dto = RequestDTO.GetFromSocket(socket);

        // Assert
        Assert.Equal(5, dto.VER);
        Assert.Equal(RequestCMD.CONNECT, dto.CMD);
        Assert.Equal(0, dto.RSV);
        Assert.Equal(AddressTYPE.IPV4, dto.ATYPE);
        Assert.Equal(ip, dto.DST_ADDR);
        Assert.Equal(new IPAddress(ip), dto.DST_ADDR_IPADDRESS);
        Assert.Equal(2 * 256 + 1, dto.DST_PORT);
    }

    [Fact]
    public void GetFromSocket_WithIPv6Address_CreatesNewInstanceFromSocket()
    {
        // Arrange
        var ip = new byte[] {
            192, 186, 201, 102,
            192, 186, 201, 102,
            192, 186, 201, 102,
            192, 186, 201, 102,
        };
        var socket = new SocketMock(new List<byte>
        {
            5, // ver
            (byte)RequestCMD.CONNECT, // cmd
            0, // rsv
            (byte)AddressTYPE.IPV6, // atype
        });
        socket.DataToReceive.AddRange(ip); // dstaddr
        socket.DataToReceive.Add(2); // dstport
        socket.DataToReceive.Add(1); // dstport


        // Act
        var dto = RequestDTO.GetFromSocket(socket);

        // Assert
        Assert.Equal(5, dto.VER);
        Assert.Equal(RequestCMD.CONNECT, dto.CMD);
        Assert.Equal(0, dto.RSV);
        Assert.Equal(AddressTYPE.IPV6, dto.ATYPE);
        Assert.Equal(ip, dto.DST_ADDR);
        Assert.Equal(new IPAddress(ip), dto.DST_ADDR_IPADDRESS);
        Assert.Equal(2 * 256 + 1, dto.DST_PORT);
    }


    [Fact]
    public void GetFromSocket_WithDomainNameAddress_CreatesNewInstanceFromSocket()
    {
        // Arrange
        var hostName = "localhost";
        var hostNameBytes = Encoding.ASCII.GetBytes(hostName);

        var socket = new SocketMock(new List<byte>
        {
            5, // ver
            (byte)RequestCMD.CONNECT, // cmd
            0, // rsv
            (byte)AddressTYPE.DOMAINNAME, // atype
            (byte)hostNameBytes.Length // dstaddrlen
        });
        socket.DataToReceive.AddRange(hostNameBytes); // dstaddr
        socket.DataToReceive.Add(2); // dstport
        socket.DataToReceive.Add(1); // dstport

        // Act
        var dto = RequestDTO.GetFromSocket(socket);

        // Assert
        Assert.Equal(5, dto.VER);
        Assert.Equal(RequestCMD.CONNECT, dto.CMD);
        Assert.Equal(0, dto.RSV);
        Assert.Equal(AddressTYPE.DOMAINNAME, dto.ATYPE);
        Assert.Equal(hostNameBytes, dto.DST_ADDR);

        // should prefer ipv4 over v6 while resolving domain names
        Assert.Equal(IPAddress.Loopback, dto.DST_ADDR_IPADDRESS);
        Assert.Equal(hostName, dto.DST_ADDR_STRING);
        Assert.Equal(2 * 256 + 1, dto.DST_PORT);
    }
}
