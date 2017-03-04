using System;
using System.Linq;
using piMasterSharp.Extensions;
using piSensorNet.Radio.NrfNet.Enums;

namespace piSensorNet.Radio.NrfNet.Registers
{
    public class TransmitObserveRegister : IRegister
    {
        public RegisterEnum Type { get; } = RegisterEnum.TransmitObserve;
        public byte Value => this;

        public byte LostPacketCount { get; set; }
        public byte RetransmittedPacketCount { get; set; }

        private TransmitObserveRegister() {}

        public TransmitObserveRegister(byte lostPacketCount, byte retransmittedPacketCount)
        {
            LostPacketCount = lostPacketCount;
            RetransmittedPacketCount = retransmittedPacketCount;
        }

        public override string ToString()
        {
            return $"[LostPacketCount: {LostPacketCount}, RetransmittedPacketCount: {RetransmittedPacketCount}]";
        }

        public static implicit operator byte(TransmitObserveRegister input)
        {
            byte output = 0;

            ByteExtensions.SetBits(ref output, 0, 4, input.RetransmittedPacketCount);
            ByteExtensions.SetBits(ref output, 4, 4, input.LostPacketCount);

            return output;
        }

        public static implicit operator TransmitObserveRegister(byte input)
        {
            var output = new TransmitObserveRegister
                         {
                             RetransmittedPacketCount = input.GetBits(0, 4),
                             LostPacketCount = input.GetBits(4, 4),
                         };

            return output;
        }
    }
}