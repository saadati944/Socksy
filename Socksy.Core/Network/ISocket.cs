namespace Socksy.Core.Network;

internal interface ISocket : IDisposable
{
    public int Send(Span<byte> data);
    public int Send(byte[] data, int offset, int length);
    public int Receive(byte[] data);
    public int Receive(Span<byte> data);
    public int Receive(byte[] data, int offset, int length);
}
