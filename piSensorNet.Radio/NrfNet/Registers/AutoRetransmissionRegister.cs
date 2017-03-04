using System;
using System.Linq;
using piMasterSharp.Extensions;
using piSensorNet.Radio.NrfNet.Enums;

namespace piSensorNet.Radio.NrfNet.Registers
{
    public class AutoRetransmissionRegister : IRegister
    {
        public RegisterEnum Type { get; } = RegisterEnum.AutoRetransmission;
        public byte Value => this;

        public AutoRetransmitDelayEnum Delay { get; set; }
        public byte Count { get; set; }

        private AutoRetransmissionRegister() {}
        
        /// <param name="delay"></param>
        /// <param name="count">Maximum value is 15.</param>
        public AutoRetransmissionRegister(AutoRetransmitDelayEnum delay, byte count)
        {
            if (count > 15)
                throw new ArgumentOutOfRangeException(nameof(count));

            Delay = delay;
            Count = count;
        }

        public override string ToString()
        {
            return $"[Delay: {Delay}, Count: {Count}]";
        }

        public static implicit operator byte(AutoRetransmissionRegister input)
        {
            byte output = 0;

            ByteExtensions.SetBits(ref output, 0, 4, input.Count);
            ByteExtensions.SetBits(ref output, 4, 4, (byte)input.Delay);

            return output;
        }

        public static implicit operator AutoRetransmissionRegister(byte input)
        {
            var output = new AutoRetransmissionRegister
                         {
                             Count = input.GetBits(0, 4),
                             Delay = (AutoRetransmitDelayEnum)input.GetBits(4, 4),
                         };

            return output;
        }
    }
}