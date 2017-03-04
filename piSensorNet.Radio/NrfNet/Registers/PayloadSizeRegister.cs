using System;
using System.Linq;
using piMasterSharp.Extensions;
using piSensorNet.Radio.NrfNet.Enums;

namespace piSensorNet.Radio.NrfNet.Registers
{
    public class PayloadSizeRegister : IRegister
    {
        public RegisterEnum Type => PipeNumber.PipeAddressRegister();
        public byte Value => this;

        public byte PipeNumber { get; }
        public byte Size { get; set; }

        private PayloadSizeRegister() {}

        /// <param name="pipeNumber">Maximum value is 5.</param>
        /// <param name="size">Value between 1 and 32 (both inclusive).</param>
        public PayloadSizeRegister(byte pipeNumber, byte size)
        {
            if (pipeNumber > 5)
                throw new ArgumentOutOfRangeException(nameof(pipeNumber));

            if (size < 1 || size > 32)
                throw new ArgumentOutOfRangeException(nameof(size));

            PipeNumber = pipeNumber;
            Size = size;
        }

        public override string ToString()
        {
            return $"[Size: {Size}]";
        }

        public static implicit operator byte(PayloadSizeRegister input)
        {
            byte output = 0;

            ByteExtensions.SetBits(ref output, 0, 6, input.Size);

            return output;
        }

        public static implicit operator PayloadSizeRegister(byte input)
        {
            var output = new PayloadSizeRegister
                         {
                             Size = input.GetBits(0, 6),
                         };

            return output;
        }
    }
}