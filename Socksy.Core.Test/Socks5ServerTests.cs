namespace Socksy.Core.Test;

public class Socks5ServerTests
{
    [Fact]
    public void ServerListensForIncommingConnectionsAfterBeingStarted()
    {
        //Arrange
        var endpoint = Fixtures.GetLocalendpointWithTemplatePortNumber();
        var sut = new Socks5Server(endpoint);

        //Act
        sut.Start();

        //Assert
        Assert.True(sut.IsListening);
    }

    [Fact]
    public async void ServerStopsAfterCallingStopMethod()
    {
        //Arrange
        var delayMs = 50;
        var endpoint = Fixtures.GetLocalendpointWithTemplatePortNumber();
        var sut = new Socks5Server(endpoint);

        //Act
        sut.Start();
        _ = Task.Delay(delayMs).ContinueWith((t) => sut.Stop());
        await sut;

        //Assert
        Assert.False(sut.IsListening);
    }

    [Fact]
    public async void ServerStopsAfterBeingDisposed()
    {
        //Arrange
        var delayMs = 50;
        var endpoint = Fixtures.GetLocalendpointWithTemplatePortNumber();
        var sut = new Socks5Server(endpoint);

        //Act
        sut.Start();
        _ = Task.Delay(delayMs).ContinueWith((t) => sut.Dispose());
        await sut;

        //Assert
        Assert.False(sut.IsListening);
    }
}
