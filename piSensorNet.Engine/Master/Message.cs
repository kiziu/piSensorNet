using System;
using System.Linq;
using piSensorNet.Radio.NrfNet;

namespace piSensorNet.Engine.Master
{
    public class Message
    {
        public Address Recipient { get; }
        public byte[] Payload { get; }
        public bool WaitForAcknowledge { get; }

        public Message(Address recipient, byte[] payload, bool waitForAcknowledge)
        {
            Recipient = recipient;
            Payload = payload;
            WaitForAcknowledge = waitForAcknowledge;
        }
    }
}