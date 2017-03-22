using System;
using System.Linq;
using piMasterSharp.Extensions;
using piSensorNet.Radio.NrfNet.Enums;

namespace piSensorNet.Radio.NrfNet.Registers
{
    public class ConfigurationRegister : IRegister
    {
        public RegisterEnum Type { get; } = RegisterEnum.Configuration;
        public byte Value => this;
        
        public bool ReceiveInterruptEnabled { get; set; }
        public bool TransmitInterruptEnabled { get; set; }
        public bool RetransmitLimitReachedInterruptEnabled { get; set; }
        public bool CrcEnabled { get; set; }
        public CrcLengthEnum CrcLength { get; set; }
        public PowerStateEnum PowerState { get; set; }
        public TransceiverModeEnum TransceiverMode { get; set; }

        private ConfigurationRegister() {}

        public ConfigurationRegister(bool receiveInterruptEnabled, bool transmitInterruptEnabled, bool retransmitLimitReachedInterruptEnabled, bool crcEnabled, CrcLengthEnum crcLength, PowerStateEnum powerState, TransceiverModeEnum transceiverMode)
        {
            ReceiveInterruptEnabled = receiveInterruptEnabled;
            TransmitInterruptEnabled = transmitInterruptEnabled;
            RetransmitLimitReachedInterruptEnabled = retransmitLimitReachedInterruptEnabled;
            CrcEnabled = crcEnabled;
            CrcLength = crcLength;
            PowerState = powerState;
            TransceiverMode = transceiverMode;
        }

        public override string ToString()
        {
            return $"[ReceiveInterruptEnabled: {ReceiveInterruptEnabled}, TransmitInterruptEnabled: {TransmitInterruptEnabled}, RetransmitLimitReachedInterruptEnabled: {RetransmitLimitReachedInterruptEnabled}, CrcEnabled: {CrcEnabled}, CrcLength: {CrcLength}, PowerState: {PowerState}, TransceiverMode: {TransceiverMode}]";
        }

        public static implicit operator byte(ConfigurationRegister input)
        {
            byte output = 0;

            ByteExtensions.SetBit(ref output, 6, !input.ReceiveInterruptEnabled);
            ByteExtensions.SetBit(ref output, 5, !input.TransmitInterruptEnabled);
            ByteExtensions.SetBit(ref output, 4, !input.RetransmitLimitReachedInterruptEnabled);
            ByteExtensions.SetBit(ref output, 3, input.CrcEnabled);
            ByteExtensions.SetBit(ref output, 2, input.CrcLength.CrcLength());
            ByteExtensions.SetBit(ref output, 1, input.PowerState.PowerState());
            ByteExtensions.SetBit(ref output, 0, input.TransceiverMode.TransceiverMode());

            return output;
        }

        public static implicit operator ConfigurationRegister(byte input)
        {
            var output = new ConfigurationRegister
                         {
                             ReceiveInterruptEnabled = !input.GetBit(6),
                             TransmitInterruptEnabled = !input.GetBit(5),
                             RetransmitLimitReachedInterruptEnabled = !input.GetBit(4),
                             CrcEnabled = input.GetBit(3),
                             CrcLength = input.GetBit(2).CrcLength(),
                             PowerState = input.GetBit(1).PowerState(),
                             TransceiverMode = input.GetBit(0).TransceiverMode(),
                         };

            return output;
        }
    }
}