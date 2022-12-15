namespace Socksy.Core.Test.Network;

public class NetHelperTests
{
    [Fact]
    public async void CreateTcpConnectionTo_WithValidEndpoint_EstablishesTcpConnection()
    {
        //Arrange
        var endpoint = Fixtures.GetLocalendpointWithTemplatePortNumber();
        var listener = new TcpListener(endpoint);

        //Act
        listener.Start();
        var listenerAcceptingClientResult = listener.AcceptTcpClientAsync();

        var client = await NetHelper.CreateTcpConnectionTo(endpoint);

        _ = await listenerAcceptingClientResult;


        //Assert
        Assert.True(client.Connected);
    }

    [Fact]
    public async void CreateTcpConnectionTo_WithValidEndpointAndEnoughTimeout_EstablishesTcpConnection()
    {
        //Arrange
        var timeout = 2000;
        var endpoint = Fixtures.GetLocalendpointWithTemplatePortNumber();
        var listener = new TcpListener(endpoint);

        //Act
        listener.Start();
        var listenerAcceptingClientResult = listener.AcceptTcpClientAsync();

        var client = await NetHelper.CreateTcpConnectionTo(endpoint, timeout);

        _ = await listenerAcceptingClientResult;


        //Assert
        Assert.True(client.Connected);
    }

    [Fact]
    public async void CreateTcpConnectionTo_AfterTimeoutReaches_ThrowsException()
    {
        //Arrange
        var timeout = 10;
        var randomPort = 54679;
        var localIP = IPAddress.Loopback;
        var endpoint = new IPEndPoint(localIP, randomPort);

        //Act
        var sut = () => NetHelper.CreateTcpConnectionTo(endpoint, timeout);

        //Assert
        await Assert.ThrowsAsync<OperationCanceledException>(sut);
    }
}
