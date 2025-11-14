using System;
using System.Runtime.CompilerServices;

namespace MemoryPack 
{
    public static class Safe
    {
        /// <summary>
        ///     Reads a value of type <typeparamref name="T" /> from the given location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadUnaligned<T>(ref byte source)
        {
            if (Environment.Is64BitProcess)
            {
                return Unsafe.ReadUnaligned<T>(ref source);
            }

            Unsafe.SkipInit(out T value);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref value), ref source, (uint)Unsafe.SizeOf<T>());
            return value;
        }

        /// <summary>
        ///     Writes a value of type <typeparamref name="T" /> to the given location.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUnaligned<T>(ref byte destination, T value)
        {
            if (Environment.Is64BitProcess)
            {
                Unsafe.WriteUnaligned(ref destination, value);
                return;
            }

            Unsafe.CopyBlockUnaligned(ref destination, ref Unsafe.As<T, byte>(ref value), (uint)Unsafe.SizeOf<T>());
        }
    }
}