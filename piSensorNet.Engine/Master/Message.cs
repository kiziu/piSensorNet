using System;
using System.Linq;
using JetBrains.Annotations;
using piSensorNet.Common.Extensions;
using piSensorNet.Radio.NrfNet;

namespace piSensorNet.Engine.Master
{
    public class Message
    {
        public int ID { get; }

        [NotNull]
        public Address Recipient { get; }

        [NotNull]
        public byte[] Payload { get; }

        public bool WithAcknowledge { get; }

        public Message(int id, [NotNull] Address recipient, [NotNull] byte[] payload, bool withAcknowledge)
        {
            if (recipient == null) throw new ArgumentNullException(nameof(recipient));
            if (payload == null) throw new ArgumentNullException(nameof(payload));

            ID = id;
            Recipient = recipient;
            Payload = payload;
            WithAcknowledge = withAcknowledge;
        }

        public Message([NotNull] Address recipient, [NotNull] byte[] payload, bool withAcknowledge)
            : this(0, recipient, payload, withAcknowledge) {}

        public override string ToString()
            => $"[ID: {ID}, Recipient: {Recipient}, Payload: {Payload.Select(i => i.ToString("X2")).Concat()}, WithAcknowledge: {WithAcknowledge}]";
    }
}