using System.Numerics;
using System.Runtime.CompilerServices;

namespace Netling.Core.Utils
{
    internal static class ByteHelpers
    {
        public static unsafe bool ContainsPossibly(byte[] buffer, int index, int end, ref Vector<short> value)
        {
            if (!Vector.IsHardwareAccelerated || index + Vector<byte>.Count + 1 >= end || index + Vector<byte>.Count + 1 >= buffer.Length)
                return true;

            fixed (byte* bufferPtr = buffer)
            {
                return
                !Vector.Equals(Unsafe.Read<Vector<short>>(bufferPtr + index), value).Equals(Vector<short>.Zero) ||
                !Vector.Equals(Unsafe.Read<Vector<short>>(bufferPtr + index + 1), value).Equals(Vector<short>.Zero);
            }
        }

        public static bool ContainsPossibly(byte[] buffer, int index, int end, ref Vector<byte> value)
        {
            if (!Vector.IsHardwareAccelerated || index + Vector<byte>.Count >= end || index + Vector<byte>.Count >= buffer.Length)
                return true;

            return !Vector.Equals(new Vector<byte>(buffer, index), value).Equals(Vector<byte>.Zero);
        }
    }
}
