namespace Socksy.Core.Test;

public class Socks5ServerTests
{
    [Fact]
    public async void ServerAcceptsIncommingConnectionsAfterBeingStarted()
    {
        //Arrange
        var options = Fixtures.GetOptionsWithLocalendpointWithTemplatePortNumber();
        var endpoint = IPEndPoint.Parse(options.EndPoint);
        var customActionCalled = false;
        var onClientConnected = (Request r) => { customActionCalled = true; };
        var client = new TcpClient();
        using var sut = new Socks5Server(onClientConnected, options: options);

        //Act
        sut.Start();
        await client.ConnectAsync(endpoint);
        var clientIsConnected = client.Connected;

        // just wait a liltle bit to let the server accept the client
        await Task.Delay(50);

        var sutIsListening = sut.IsListening;
        client.Close();

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
        var options = Fixtures.GetOptionsWithLocalendpointWithTemplatePortNumber();
        using var sut = new Socks5Server(options: options);

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
        var options = Fixtures.GetOptionsWithLocalendpointWithTemplatePortNumber();
        using var sut = new Socks5Server(options: options);

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
        var options = Fixtures.GetOptionsWithLocalendpointWithTemplatePortNumber();
        using var sut = new Socks5Server(options: options);

        //Act
        sut.Start();
        _ = Task.Delay(delayMs).ContinueWith((t) => sut.Dispose());
        await sut;

        //Assert
        Assert.False(sut.IsListening);
    }
}
