namespace Socksy.Core.Test.Dtos;

public class SetMethodDTOTests
{
    [Fact]
    public void Send_SendsDtoOverSocket()
    {
        // Arrange
        byte ver = 5;
        AuthenticationMETHOD method = AuthenticationMETHOD.NO_ACCEPTABLE_METHODS;
        var dto = SetMethodDTO.Create(ver, method);
        var socket = new SocketMock();
        
        // Act
        dto.Send(socket);

        // Assert
        Assert.Equal(new byte[] { ver, (byte)method }, socket.SentData);
    }
}
