using System;
using System.Linq;
using piMasterSharp.Extensions;
using piSensorNet.Radio.NrfNet.Enums;

namespace piSensorNet.Radio.NrfNet.Registers
{
    public class RadioRegister : IRegister
    {
        public RegisterEnum Type { get; } = RegisterEnum.Radio;
        public byte Value => this;

        public bool ContinuousCarrierTransmitEnabled { get; set; }
        public bool PllLockEnabled { get; set; }
        public DataRateEnum DataRate { get; set; }
        public OutputPowerEnum OutputPower { get; set; }

        private RadioRegister() {}

        public RadioRegister(bool continuousCarrierTransmitEnabled, bool pllLockEnabled, DataRateEnum dataRate, OutputPowerEnum outputPower)
        {
            ContinuousCarrierTransmitEnabled = continuousCarrierTransmitEnabled;
            PllLockEnabled = pllLockEnabled;
            DataRate = dataRate;
            OutputPower = outputPower;
        }

        public override string ToString()
        {
            return $"[ContinuousCarrierTransmitEnabled: {ContinuousCarrierTransmitEnabled}, PllLockEnabled: {PllLockEnabled}, DataRate: {DataRate}, OutputPower: {OutputPower}]";
        }

        public static implicit operator byte(RadioRegister input)
        {
            byte output = 0;

            ByteExtensions.SetBits(ref output, 1, 2, (byte)input.OutputPower);
            ByteExtensions.SetBits(ref output, 3, 3, (byte)input.DataRate);
            ByteExtensions.SetBit(ref output, 4, input.PllLockEnabled);
            ByteExtensions.SetBit(ref output, 7, input.ContinuousCarrierTransmitEnabled);

            return output;
        }

        public static implicit operator RadioRegister(byte input)
        {
            var output = new RadioRegister
                         {
                             DataRate = (DataRateEnum)(input.GetBits(3, 3) & 5),
                             OutputPower = (OutputPowerEnum)input.GetBits(1, 2),
                             PllLockEnabled = input.GetBit(4),
                             ContinuousCarrierTransmitEnabled = input.GetBit(7),
                         };
            return output;
        }
    }
}