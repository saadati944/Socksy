namespace Socksy.Core.Test.Dtos;

public class SetMethodDTOTests
{
    [Fact]
    public void Send_SendsDtoOverSocket()
    {
        // Arrange
        byte ver = 5;
        byte method = 0;
        var dto = SetMethodDTO.Create(ver, method);
        var socket = new SocketMock();
        
        // Act
        dto.Send(socket);

        // Assert
        Assert.Equal(new byte[] { ver, method }, socket.SentData);
    }
}
