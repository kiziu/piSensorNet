using System;
using System.Linq;

namespace piSensorNet.Radio.NrfNet.Enums
{
    public enum CommandEnum : byte
    {
        ReadRegister = 0x00,
        WriteRegister = 0x20,
        ReadReceivedPayload = 0x61,
        WriteTransmittedPayload = 0xA0,
        FlushTransmitQueue = 0xE1,
        FlushReceiveQueue = 0xE2,
        ReuseLastTransmittedPayload = 0xE3,
        ReadReceivedPayloadWidth = 0x60,
        WriteAcknowledgePayload = 0xA8,
        WriteTransmittedPayloadWithoutAcknowledge = 0xB0,
        NoOperation = 0xFF,
    }
}