using System;
using System.Linq;
using piMasterSharp.Extensions;
using piSensorNet.Radio.NrfNet.Enums;

namespace piSensorNet.Radio.NrfNet.Registers
{
    public class FrequencyChannelRegister : IRegister
    {
        public RegisterEnum Type { get; } = RegisterEnum.FrequencyChannel;
        public byte Value => this;

        public byte Channel { get; set; }

        public ulong Frequency => 2400000000U + Channel * 1000000U;

        private FrequencyChannelRegister() {}

        /// <param name="channel">Maximum value is 127.</param>
        public FrequencyChannelRegister(byte channel)
        {
            if (channel > 127)
                throw new ArgumentOutOfRangeException(nameof(channel));

            Channel = channel;
        }

        public override string ToString()
        {
            return $"[Channel: {Channel}, _Frequency: {Frequency}]";
        }

        public static implicit operator byte(FrequencyChannelRegister input)
        {
            byte output = 0;

            ByteExtensions.SetBits(ref output, 0, 7, input.Channel);

            return output;
        }

        public static implicit operator FrequencyChannelRegister(byte input)
        {
            var output = new FrequencyChannelRegister
                         {
                             Channel = input.GetBits(0, 7),
                         };

            return output;
        }
    }
}