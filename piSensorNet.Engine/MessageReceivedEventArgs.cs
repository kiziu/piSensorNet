using System;
using System.Linq;
using JetBrains.Annotations;
using piSensorNet.Radio.NrfNet;

namespace piSensorNet.Engine
{
    public sealed class MessageReceivedEventArgs : EventArgs
    {
        [NotNull]
        public Address Sender { get; }

        public byte ChunksCount { get; }

        [NotNull]
        public string Message { get; }

        public DateTime FirstPacketReceived { get; }
        public DateTime LastPacketReceived { get; }

        public TimeSpan TimeTaken => LastPacketReceived - FirstPacketReceived;

        public MessageReceivedEventArgs([NotNull] Address sender, byte chunksCount, [NotNull] string message, DateTime firstPacketReceived, DateTime lastPacketReceived)
        {
            if (sender == null) throw new ArgumentNullException(nameof(sender));
            if (message == null) throw new ArgumentNullException(nameof(message));

            Sender = sender;
            ChunksCount = chunksCount;
            Message = message;
            FirstPacketReceived = firstPacketReceived;
            LastPacketReceived = lastPacketReceived;
        }
    }
}