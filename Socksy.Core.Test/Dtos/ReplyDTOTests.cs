namespace Socksy.Core.Test.Dtos;

public class ReplyDTOTests
{
    [Fact]
    public void Send_WithIPv4_SendsDtoOverSocket()
    {
        // Arrange
        var ip = new byte[] { 192, 186, 201, 102 };
        ushort port = 2 * 256 + 1;

        var dto = ReplyDTO.Create(5, ReplyREP.Succeeded, AddressTYPE.IPV4, ip, port);
        var socket = new SocketMock();

        // Act
        dto.Send(socket);

        // Assert
        var expected = new List<byte>
        {
            5, // ver
            (byte) ReplyREP.Succeeded, // rep
            0, // rsv
            (byte) AddressTYPE.IPV4, // atype
        };
        expected.AddRange(ip); // bndaddr
        expected.Add(2); // bndport
        expected.Add(1); // bndport

        Assert.Equal(expected, socket.SentData);
    }

    [Fact]
    public void Send_WithIPv6_SendsDtoOverSocket()
    {
        // Arrange
        var ip = new byte[] {
            192, 186, 201, 102,
            192, 186, 201, 102,
            192, 186, 201, 102,
            192, 186, 201, 102
        };
        ushort port = 2 * 256 + 1;

        var dto = ReplyDTO.Create(5, ReplyREP.Succeeded, AddressTYPE.IPV6, ip, port);
        var socket = new SocketMock();

        // Act
        dto.Send(socket);

        // Assert
        var expected = new List<byte>
        {
            5, // ver
            (byte) ReplyREP.Succeeded, // rep
            0, // rsv
            (byte) AddressTYPE.IPV6, // atype
        };
        expected.AddRange(ip); // bndaddr
        expected.Add(2); // bndport
        expected.Add(1); // bndport

        Assert.Equal(expected, socket.SentData);
    }
}
