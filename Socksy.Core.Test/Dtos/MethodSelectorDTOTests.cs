namespace Socksy.Core.Test.Dtos;

public class MethodSelectorDTOTests
{
    [Fact]
    public void GetFromSocket_CreatesInstanceFromSocket()
    {
        // Arrange
        var socket = new SocketMock(new List<byte>{
            5, // ver
            2, // nmethods
            0, 1 // methods
        });
        
        // Act
        var dto = MethodSelectorDTO.GetFromSocket(socket);

        // Assert
        Assert.Equal(5, dto.VER);
        Assert.Equal(2, dto.NMETHODS);
        Assert.Equal(new byte[] {0, 1}, dto.METHODS);
    }
}
