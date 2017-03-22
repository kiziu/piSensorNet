using System;
using System.Linq;
using piMasterSharp.Extensions;
using piSensorNet.Radio.NrfNet.Enums;

namespace piSensorNet.Radio.NrfNet.Registers
{
    public sealed class StatusRegister : IRegister
    {
        public RegisterEnum Type { get; } = RegisterEnum.Status;
        public byte Value => this;

        public bool DataReady { get; set; }
        public bool DataSent { get; set; }
        public bool RetransmitLimitReached { get; set; }
        public byte? DataReadyPipeNumber { get; private set; }
        public bool TransmitQueueFull { get; private set; }

        private StatusRegister() {}

        public StatusRegister(bool dataReady, bool dataSent, bool retransmitLimitReached)
        {
            DataReady = dataReady;
            DataSent = dataSent;
            RetransmitLimitReached = retransmitLimitReached;
            DataReadyPipeNumber = null;
            TransmitQueueFull = false;
        }

        public override string ToString()
        {
            return $"[DataReady: {DataReady}, DataSent: {DataSent}, RetransmitLimitReached: {RetransmitLimitReached}, DataReadyPipeNumber: {DataReadyPipeNumber}, TransmitQueueFull: {TransmitQueueFull}]";
        }

        public static implicit operator byte(StatusRegister input)
        {
            byte output = 0;

            ByteExtensions.SetBit(ref output, 6, input.DataReady);
            ByteExtensions.SetBit(ref output, 5, input.DataSent);
            ByteExtensions.SetBit(ref output, 4, input.RetransmitLimitReached);

            return output;
        }

        public static implicit operator StatusRegister(byte input)
        {
            var output = new StatusRegister
                         {
                             DataReady = input.GetBit(6),
                             DataSent = input.GetBit(5),
                             RetransmitLimitReached = input.GetBit(4),
                             DataReadyPipeNumber = input.GetBits(1, 3),
                             TransmitQueueFull = input.GetBit(0)
                         };

            if (output.DataReadyPipeNumber == 7) // empty
                output.DataReadyPipeNumber = null;

            return output;
        }
    }
}