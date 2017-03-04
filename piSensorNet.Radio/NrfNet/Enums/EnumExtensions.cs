using System;
using System.Linq;

namespace piSensorNet.Radio.NrfNet.Enums
{
    internal static class EnumExtensions
    {
        public static CrcLengthEnum CrcLength(this bool input)
        {
            return input ? CrcLengthEnum.TwoBytes : CrcLengthEnum.OneByte;
        }

        public static bool CrcLength(this CrcLengthEnum input)
        {
            return input == CrcLengthEnum.TwoBytes;
        }

        public static PowerStateEnum PowerState(this bool input)
        {
            return input ? PowerStateEnum.Up : PowerStateEnum.Down;
        }

        public static bool PowerState(this PowerStateEnum input)
        {
            return input == PowerStateEnum.Up;
        }

        public static TransceiverModeEnum TransceiverMode(this bool input)
        {
            return input ? TransceiverModeEnum.Receiver : TransceiverModeEnum.Transmitter;
        }

        public static bool TransceiverMode(this TransceiverModeEnum input)
        {
            return input == TransceiverModeEnum.Receiver;
        }

        public static RegisterEnum PipeAddressRegister(this byte pipeNumber)
        {
            switch (pipeNumber)
            {
                case 0:
                    return RegisterEnum.Pipe0ReceiveAddress;
                case 1:
                    return RegisterEnum.Pipe1ReceiveAddress;
                case 2:
                    return RegisterEnum.Pipe2ReceiveAddress;
                case 3:
                    return RegisterEnum.Pipe3ReceiveAddress;
                case 4:
                    return RegisterEnum.Pipe4ReceiveAddress;
                case 5:
                    return RegisterEnum.Pipe5ReceiveAddress;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pipeNumber));
            }
        }
    }
}