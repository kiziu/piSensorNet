using System;
using System.Linq;

namespace piSensorNet.Radio.NrfNet.Enums
{
    public enum RegisterEnum : byte
    { // size of 1 byte, unless indicated otherwise
        Configuration = 0,
        AutoAcknowledgment = 1,
        ReceiverAddress = 2,
        AddressWidth = 3,
        AutoRetransmission = 4,
        FrequencyChannel = 5,
        Radio = 6,
        Status = 7,
        TransmitObserve = 8,
        ReceivedPowerDetector = 9,
        Pipe0ReceiveAddress = 10, // 5 bytes, LSB first
        Pipe1ReceiveAddress = 11, // 5 bytes, LSB first
        Pipe2ReceiveAddress = 12, // 1 byte - only LSB, MSBs from Pipe 1
        Pipe3ReceiveAddress = 13, // 1 byte - only LSB, MSBs from Pipe 1
        Pipe4ReceiveAddress = 14, // 1 byte - only LSB, MSBs from Pipe 1
        Pipe5ReceiveAddress = 15, // 1 byte - only LSB, MSBs from Pipe 1
        TransmitAddress = 16, // 5 bytes, LSB first
        Pipe0PayloadSize = 17, // value of 1 - 32
        Pipe1PayloadSize = 18, // value of 1 - 32
        Pipe2PayloadSize = 19, // value of 1 - 32
        Pipe3PayloadSize = 20, // value of 1 - 32
        Pipe4PayloadSize = 21, // value of 1 - 32
        Pipe5PayloadSize = 22, // value of 1 - 32
        FifoStatus = 23,
        DynamicPayloadLength = 28,
        Feature = 29,
    }
}