using System;
using System.Linq;
using JetBrains.Annotations;

namespace piSensorNet.Engine.Master
{
    public sealed class PayloadReceivedEventArgs : EventArgs
    {
        [NotNull]
        public byte[] Payload { get; }

        public DateTime Received { get; }

        public PayloadReceivedEventArgs([NotNull] byte[] payload, DateTime received)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));

            Payload = payload;
            Received = received;
        }
    }
}