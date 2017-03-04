using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace piMasterSharp.Extensions
{
    public static class ByteExtensions
    {
        private static readonly byte[] MultiBitMasks =
        {
            0,
            1,
            1 + 2,
            1 + 2 + 4,
            1 + 2 + 4 + 8,
            1 + 2 + 4 + 8 + 16,
            1 + 2 + 4 + 8 + 16 + 32,
            1 + 2 + 4 + 8 + 16 + 32 + 64,
            1 + 2 + 4 + 8 + 16 + 32 + 64 + 128
        };

        private static readonly byte[] BitMasks =
        {
            1,
            2,
            4,
            8,
            16,
            32,
            64,
            128
        };

        private static readonly byte[] InversedBitMasks =
        {
            254,
            253,
            251,
            247,
            239,
            223,
            191,
            127
        };
        
        public static void SetBit(ref byte value, int bitNumber, bool state)
        {
            if (state)
                value = (byte)(value | BitMasks[bitNumber]);
            else
                value = (byte)(value & InversedBitMasks[bitNumber]);
        }

        public static void SetBits(ref byte output, int firstBit, int numberOfBits, byte value)
        {
            var setMask = MultiBitMasks[numberOfBits];
            var clearMask = (byte)(~(setMask << firstBit));

            value &= setMask;
            value <<= firstBit;

            output &= clearMask;
            output |= value;
        }

        public static bool GetBit(this byte value, int bitNumber)
        {
            return (value & (1 << bitNumber)) > 0;
        }

        public static byte GetBits(this byte value, int firstBit, int numberOfBits)
        {
            var setMask = (byte)(MultiBitMasks[numberOfBits] << firstBit);

            value &= setMask;
            value >>= firstBit;

            return value;
        }

        public static string ToBits(this byte value)
        {
            var builder = new StringBuilder(8);
            var bit = 128;

            while (bit > 0)
            {
                builder.Append((value & bit) > 0 ? "1" : "0");

                bit >>= 1;
            }
            
            return builder.ToString();
        }

        public static byte[] StructureToBytes<T>(ref T o, int arraySize)
            where T : struct
        {
            var size = Marshal.SizeOf(o);
            var bytes = new byte[arraySize];
            var pointer = Marshal.AllocHGlobal(size);

            Marshal.StructureToPtr(o, pointer, true);
            Marshal.Copy(pointer, bytes, 0, size);

            return bytes;
        }

        public static T BytesToStructure<T>(byte[] bytes)
            where T : struct
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));

            handle.Free();

            return structure;
        }
    }
}

