namespace Socksy.Core.Test.Dtos;

internal class SocketMock : ISocket
{
    private int _receiveOffset;
    private List<byte> _sentData = new List<byte>();
    private bool _disposed = false;

    public List<byte> DataToReceive { get; set; }
    public List<byte> SentData => _sentData;
    public bool Disposed => _disposed;

    public bool Connected => throw new NotImplementedException();

    public int Available => throw new NotImplementedException();

    public int ReceiveTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public int SendTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public IPEndPoint RemoteEndPoint => throw new NotImplementedException();

    public SocketMock()
    {
        DataToReceive = new();
    }

    public SocketMock(List<byte> dataToReceive)
    {
        DataToReceive = dataToReceive;
    }

    public int Receive(byte[] data)
    {
        Array.Copy(DataToReceive.Skip(_receiveOffset).Take(data.Length).ToArray(), data, data.Length);
        _receiveOffset += data.Length;
        return data.Length;
    }

    public int Receive(Span<byte> data)
    {
        var i = 0;
        foreach(var b in DataToReceive.Skip(_receiveOffset).Take(data.Length))
            data[i++] = b;

        _receiveOffset += data.Length;
        return data.Length;
    }

    public int Receive(byte[] data, int offset, int length)
    {
        Array.Copy(DataToReceive.Skip(_receiveOffset).Take(length).ToArray(), 0, data, offset, length);
        _receiveOffset += length;
        return length;
    }

    public int Send(Span<byte> data)
    {
        _sentData.AddRange(data.ToArray());
        return data.Length;
    }

    public int Send(byte[] data, int offset, int length)
    {
        _sentData.AddRange(data.Skip(offset).Take(length).ToArray());
        return length;
    }

    public void Dispose()
    {
        _disposed = true;
    }

    public bool Poll(int v, SelectMode selectRead)
    {
        throw new NotImplementedException();
    }

    public void Close()
    {
        throw new NotImplementedException();
    }
}
