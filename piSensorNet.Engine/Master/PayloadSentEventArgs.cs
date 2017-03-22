using System;
using System.Linq;
using JetBrains.Annotations;

namespace piSensorNet.Engine.Master
{
    public sealed class PayloadSentEventArgs : EventArgs
    {
        [NotNull]
        public Message Message { get; }

        public byte RetransmissionsCount { get;  }

        public DateTime Sent { get;  }

        public PayloadSentEventArgs([NotNull] Message message, byte retransmissionsCount, DateTime sent)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            Message = message;
            RetransmissionsCount = retransmissionsCount;
            Sent = sent;
        }
    }
}