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
        public bool TransmitQueueFull { get; set; }
        public bool TransmitQueueEmpty { get; set; }
        public bool ReceiveQueueFull { get; set; }
        public bool ReceiveQueueEmpty { get; set; }

        private FifoStatusRegister() {}

        public FifoStatusRegister(bool reuseLastTransmitPayload, bool transmitQueueFull, bool transmitQueueEmpty, bool receiveQueueFull, bool receiveQueueEmpty)
        {
            ReuseLastTransmitPayload = reuseLastTransmitPayload;
            TransmitQueueFull = transmitQueueFull;
            TransmitQueueEmpty = transmitQueueEmpty;
            ReceiveQueueFull = receiveQueueFull;
            ReceiveQueueEmpty = receiveQueueEmpty;
        }

        public override string ToString()
        {
            return $"[ReuseLastTransmitPayload: {ReuseLastTransmitPayload}, TransmitQueueFull: {TransmitQueueFull}, TransmitQueueEmpty: {TransmitQueueEmpty}, ReceiveQueueFull: {ReceiveQueueFull}, ReceiveQueueEmpty: {ReceiveQueueEmpty}]";
        }

        public static implicit operator byte(FifoStatusRegister input)
        {
            byte output = 0;

            ByteExtensions.SetBit(ref output, 6, input.ReuseLastTransmitPayload);
            ByteExtensions.SetBit(ref output, 5, input.TransmitQueueFull);
            ByteExtensions.SetBit(ref output, 4, input.TransmitQueueEmpty);
            ByteExtensions.SetBit(ref output, 1, input.ReceiveQueueFull);
            ByteExtensions.SetBit(ref output, 0, input.ReceiveQueueEmpty);

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