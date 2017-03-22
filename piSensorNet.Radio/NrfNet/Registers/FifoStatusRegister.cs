using System;
using System.Linq;
using piMasterSharp.Extensions;
using piSensorNet.Radio.NrfNet.Enums;

namespace piSensorNet.Radio.NrfNet.Registers
{
    public class FifoStatusRegister : IRegister
    {
        public RegisterEnum Type { get; } = RegisterEnum.FifoStatus;
        public byte Value => this;

        public bool ReuseLastTransmitPayload { get; set; }
        public bool TransmitQueueFull { get; private set; }
        public bool TransmitQueueEmpty { get; private set; }
        public bool ReceiveQueueFull { get; private set; }
        public bool ReceiveQueueEmpty { get; private set; }

        private FifoStatusRegister() {}

        public FifoStatusRegister(bool reuseLastTransmitPayload)
        {
            ReuseLastTransmitPayload = reuseLastTransmitPayload;
            TransmitQueueFull = false;
            TransmitQueueEmpty = false;
            ReceiveQueueFull = false;
            ReceiveQueueEmpty = false;
        }

        public override string ToString()
        {
            return $"[ReuseLastTransmitPayload: {ReuseLastTransmitPayload}, TransmitQueueFull: {TransmitQueueFull}, TransmitQueueEmpty: {TransmitQueueEmpty}, ReceiveQueueFull: {ReceiveQueueFull}, ReceiveQueueEmpty: {ReceiveQueueEmpty}]";
        }

        public static implicit operator byte(FifoStatusRegister input)
        {
            byte output = 0;

            ByteExtensions.SetBit(ref output, 6, input.ReuseLastTransmitPayload);

            return output;
        }

        public static implicit operator FifoStatusRegister(byte input)
        {
            var output = new FifoStatusRegister
                         {
                             ReuseLastTransmitPayload = input.GetBit(6),
                             TransmitQueueFull = input.GetBit(5),
                             TransmitQueueEmpty = input.GetBit(4),
                             ReceiveQueueFull = input.GetBit(1),
                             ReceiveQueueEmpty = input.GetBit(0),
                         };

            return output;
        }
    }
}