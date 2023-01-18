using System.Net.Sockets;

namespace Socksy.Core.Network;

internal struct SocketWrapper : ISocket, IDisposable
{
    private Socket _socket;

    public bool Connected => _socket.Connected;

    public int Available => _socket.Available;

    public int ReceiveTimeout
    {
        get
        {
            return _socket.ReceiveTimeout;
        }
        set
        {
            _socket.ReceiveTimeout = value;
        }
    }

    public IPEndPoint RemoteEndPoint
        => _socket.RemoteEndPoint as IPEndPoint;

    public int SendTimeout
    {
        get
        {
            return _socket.SendTimeout;
        }
        set
        {
            _socket.SendTimeout = value;
        }
    }

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
        Span<byte> buff = stackalloc byte[length];
        int received = _socket.Receive(buff);
        buff.CopyTo(data.AsSpan(offset, length));
        return received;
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
        Span<byte> buff = data.AsSpan(offset, length);
        return _socket.Send(buff);
    }

    public void Dispose()
    {
        _socket.Dispose();
    }

    public bool Poll(int v, SelectMode selectRead)
    {
        return _socket.Poll(v, selectRead);
    }

    public void Close()
    {
        _socket.Close();
    }
}
