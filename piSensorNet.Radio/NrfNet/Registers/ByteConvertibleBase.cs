using System;
using System.Linq;

namespace piSensorNet.Radio.NrfNet.Registers
{
    public abstract class ByteConvertibleBase
    {
        private static readonly Type[] ByteType = {typeof(byte)};

        public abstract byte ToByte();

        public static T FromByte<T>(byte input)
            where T : ByteConvertibleBase
        {
            var type = typeof(T);
            var castOperator = type.GetMethod("op_Implicit", ByteType);
            var castFunction = (Func<byte, T>)Delegate.CreateDelegate(typeof(Func<byte, T>), castOperator);

            var result = castFunction(input);

            return result;
        }
    }
}