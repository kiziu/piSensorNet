using System;
using System.Linq;

namespace piSensorNet.Radio.NrfNet.Enums
{
    public enum CommandEnum : byte
    {
        ReadRegister = 0x00,
        WriteRegister = 0x20,
        ReadPayload = 0x61,
        WritePayload = 0xA0,
        FlushTransmitQueue = 0xE1,
        FlushReceiveQueue = 0xE2,
        ReuseLastTransmittedPayload = 0xE3,
        ReadReceivedPayloadWidth = 0x60,
        WriteAcknowledgePayload = 0xA8,
        WritePayloadNoAcknowledge = 0xB0,
        NoOperation = 0xFF,
    }
}