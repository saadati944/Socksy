namespace Socksy.Core.Network;

internal class SocketWrapper : ISocket, IDisposable
{
    private Socket _socket;

    public SocketWrapper(Socket socket)
    {
        _socket = socket;
    }

    public int Receive(byte[] data)
    {
        return _socket.Receive(data);
    }

    public int Receive(byte[] data, int offset, int length)
    {
        return _socket.Receive(data, offset, length, SocketFlags.None);
    }

    public int Receive(Span<byte> data)
    {
        return _socket.Receive(data);
    }

    public int Send(Span<byte> data)
    {
        return _socket.Send(data);
    }

    public int Send(byte[] data, int offset, int length)
    {
        return _socket.Send(data, offset, length, SocketFlags.None);
    }

    public void Dispose()
    {
        _socket.Dispose();
    }
}
