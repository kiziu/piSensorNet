using System;
using System.Linq;

namespace piSensorNet.Engine
{
    public sealed class MessageSendingFailedEventArgs : EventArgs
    {
        public int ID { get; }
        public byte Sequence { get; }
        public byte Current { get; }
        public byte Total { get; }

        public MessageSendingFailedEventArgs(int id, byte sequence, byte current, byte total)
        {
            ID = id;
            Sequence = sequence;
            Current = current;
            Total = total;
        }
    }
}