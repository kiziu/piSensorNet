using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace piSensorNet.Engine
{
    public sealed class MessageSentEventArgs : EventArgs
    {
        public int ID { get; }
        public byte ChunksCount { get; }

        [NotNull]
        public IReadOnlyList<byte> RetransmissionsCounts { get; }
        public DateTime FirstPacketSent { get; }
        public DateTime LastPacketSent { get; }
        
        public TimeSpan TimeTaken => LastPacketSent - FirstPacketSent;

        public MessageSentEventArgs(int id, byte chunksCount, [NotNull] IReadOnlyList<byte> retransmissionsCounts, DateTime firstPacketSent, DateTime lastPacketSent)
        {
            if (retransmissionsCounts == null) throw new ArgumentNullException(nameof(retransmissionsCounts));

            ID = id;
            ChunksCount = chunksCount;
            RetransmissionsCounts = retransmissionsCounts;
            FirstPacketSent = firstPacketSent;
            LastPacketSent = lastPacketSent;
        }
    }
}