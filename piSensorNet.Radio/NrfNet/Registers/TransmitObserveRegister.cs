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

        public byte LostPacketCount { get; private set; }
        public byte RetransmittedPacketCount { get; private set; }

        public TransmitObserveRegister()
        {
            LostPacketCount = 0;
            RetransmittedPacketCount = 0;
        }

        public override string ToString()
        {
            return $"[LostPacketCount: {LostPacketCount}, RetransmittedPacketCount: {RetransmittedPacketCount}]";
        }

        public static implicit operator byte(TransmitObserveRegister input) 
            => 0;

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