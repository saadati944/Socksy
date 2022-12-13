using Socksy.Core.Server;
using Socksy.Core.Network;

namespace Socksy.Core.Test;

public class Socks5ServerTests
{
    [Fact]
    public async void ServerAcceptsIncommingConnectionsAfterBeingStarted()
    {
        //Arrange
        var endpoint = Fixtures.GetLocalendpointWithTemplatePortNumber();
        var customActionCalled = false;
        var onClientConnected = (Request r) => { customActionCalled = true; };
        var client = new TcpClient();
        using var sut = new Socks5Server(endpoint, onClientConnected);

        //Act
        sut.Start();
        await client.ConnectAsync(endpoint);
        var clientIsConnected = client.Connected;

        // just wait a liltle bit to let the server accept the client
        await Task.Delay(50);

        var sutIsListening = sut.IsListening;

        sut.Stop();
        await sut;

        //Assert
        Assert.True(sutIsListening);
        Assert.True(clientIsConnected);
        Assert.True(customActionCalled);
    }

    [Fact]
    public void ServerListensForIncommingConnectionsAfterBeingStarted()
    {
        //Arrange
        var endpoint = Fixtures.GetLocalendpointWithTemplatePortNumber();
        using var sut = new Socks5Server(endpoint);

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
        using var sut = new Socks5Server(endpoint);

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
        using var sut = new Socks5Server(endpoint);

        //Act
        sut.Start();
        _ = Task.Delay(delayMs).ContinueWith((t) => sut.Dispose());
        await sut;

        //Assert
        Assert.False(sut.IsListening);
    }
}
