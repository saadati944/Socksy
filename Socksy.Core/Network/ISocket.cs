namespace Socksy.Core.Network;

internal interface ISocket : IDisposable
{
    bool Connected { get; }
    int Available { get; }
    int ReceiveTimeout { get; set; }
    int SendTimeout { get; set; }
    public IPEndPoint RemoteEndPoint { get; }

    public int Send(Span<byte> data);
    public int Send(byte[] data, int offset, int length);
    public int Receive(byte[] data);
    public int Receive(Span<byte> data);
    public int Receive(byte[] data, int offset, int length);
    bool Poll(int v, SelectMode selectRead);
    void Close();
}
