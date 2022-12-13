using System.Buffers;

namespace Socksy.Core.Dtos;

internal static class Helper
{
    public static ushort GetPort(byte[] data)
        => (ushort)(data[0] * 256 + data[1]);

    public static byte[] RentByteArray(int minLength)
        => ArrayPool<byte>.Shared.Rent(minLength);

    public static void ReturnByteArray(byte[] data)
        => ArrayPool<byte>.Shared.Return(data);
}
