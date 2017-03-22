using System;
using System.Linq;
using piMasterSharp.Extensions;
using piSensorNet.Radio.NrfNet.Enums;

namespace piSensorNet.Radio.NrfNet.Registers
{
    public class ReceivedPowerDetectorRegister : IRegister
    {
        public RegisterEnum Type { get; } = RegisterEnum.ReceivedPowerDetector;
        public byte Value => this;

        public bool ReceivedPowerDetector { get; private set; }

        public ReceivedPowerDetectorRegister() {}

        public override string ToString()
        {
            return $"[ReceivedPowerDetector: {ReceivedPowerDetector}]";
        }

        public static implicit operator byte(ReceivedPowerDetectorRegister input)
            => 0;

        public static implicit operator ReceivedPowerDetectorRegister(byte input)
        {
            var output = new ReceivedPowerDetectorRegister
                         {
                             ReceivedPowerDetector = input.GetBit(0),
                         };

            return output;
        }
    }
}