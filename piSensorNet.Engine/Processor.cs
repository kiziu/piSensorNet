using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using piSensorNet.Common.Extensions;
using piSensorNet.Engine.Master;
using piSensorNet.Radio.NrfNet;
using piSensorNet.WiringPi.Enums;

namespace piSensorNet.Engine
{
    public sealed class Processor : IDisposable
    {
        private readonly Module _module;

        private readonly Dictionary<MessageKey, ReceivedContext> _receivedContexts = new Dictionary<MessageKey, ReceivedContext>(10);
        private readonly Dictionary<int, SentContext> _sentContexts = new Dictionary<int, SentContext>(10);

        private byte _sequence;

        public event EventHandler<MessageReceivedEventArgs> Received;
        public event EventHandler<MessageSentEventArgs> Sent;
        public event EventHandler<MessageSendingFailedEventArgs> Failed;
        public event EventHandler<ErrorEventArgs> Error;

        public Address Address => _module.Address;
        public Address BroadcastAddress => _module.BroadcastAddress;

        public Processor(PinNumberEnum chipEnable, SpiChannelEnum channel, [NotNull] Address address, [NotNull] Address broadcastAddress, byte radioChannel,
            bool sendContinuously = false, bool readRetransmissionsCount = false)
        {
            _module = new Module(chipEnable, channel, address, broadcastAddress, radioChannel, sendContinuously, readRetransmissionsCount,
                spiSpeed: 12000000);

            _module.Received += OnReceived;
            _module.SendingFailed += OnSendingFailed;
            _module.Sent += OnSent;
            _module.Error += OnError;
        }

        public byte EnqueueForSending(int id, [NotNull] Address recipient, [NotNull] string message, bool withAcknowledge = true)
        {
            if (recipient == null) throw new ArgumentNullException(nameof(recipient));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var chunks = message.Chunkify(Packet.MessageSize);
            var chunksCount = (byte)chunks.Count;

            _sentContexts.Add(id, new SentContext(chunksCount));

            for (byte i = 0; i < chunksCount; i++)
            {
                var internalMessage = new Message(id, recipient, (byte[])new Packet(_sequence, _module.Address, (byte)(i + 1), chunksCount, chunks[i]), withAcknowledge);

                _module.EnqueueForSending(internalMessage);
            }

            unchecked
            {
                ++_sequence;
            }

            return chunksCount;
        }

        public void Start(bool waitForWorker = false)
        {
            if (Received == null)
                throw new InvalidOperationException($"Event {nameof(Received)} has no listeners. Attach at least one before starting.");

            _module.Start(waitForWorker);
        }

        public void Dispose()
            => _module.Dispose();

        public sealed class Status
        {
            public byte Sequence { get; }
            public Dictionary<MessageKey, ReceivedContext> ReceivedContexts { get; }
            public Dictionary<int, SentContext> SentContexts { get; set; }

            public Status(byte sequence, Dictionary<MessageKey, ReceivedContext> receivedContexts, Dictionary<int, SentContext> sentContexts)
            {
                Sequence = sequence;
                ReceivedContexts = receivedContexts;
                SentContexts = sentContexts;
            }
        }

        public Status GetStatus() => new Status(_sequence, _receivedContexts, _sentContexts);

        private void OnError(object sender, ErrorEventArgs args)
            => Error?.Invoke(this, args);

        private void OnSent(object sender, PayloadSentEventArgs args)
        {
            var message = args.Message;
            var id = message.ID;
            var packet = (Packet)message.Payload;
            var sentContext = _sentContexts[id];

            sentContext.RetransmissionsCounts[packet.Current - 1] = args.RetransmissionsCount;

            if (sentContext.Total == 1)
            {
                Sent?.Invoke(this, new MessageSentEventArgs(id, sentContext.Total, sentContext.RetransmissionsCounts, args.Sent, args.Sent));
                _sentContexts.Remove(id);

                return;
            }

            if (sentContext.Count == 0)
                sentContext.FirstSent = args.Sent;

            ++sentContext.Count;

            if (sentContext.Count != packet.Current)
            {
                Failed?.Invoke(this, new MessageSendingFailedEventArgs(id, _sequence, packet.Current, packet.Total));
                _sentContexts.Remove(id);

                return;
            }

            if (sentContext.Count != sentContext.Total)
                return;

            Sent?.Invoke(this, new MessageSentEventArgs(id, sentContext.Total, sentContext.RetransmissionsCounts, sentContext.FirstSent, args.Sent));
            _sentContexts.Remove(id);
        }

        private void OnSendingFailed(object sender, PayloadSendingFailedEventArgs args)
        {
            var packet = (Packet)args.Message.Payload;

            Failed?.Invoke(this, new MessageSendingFailedEventArgs(args.Message.ID, packet.Sequence, packet.Current, packet.Total));
        }

        private void OnReceived(object sender, PayloadReceivedEventArgs args)
        {
            // TODO KZ: handle missing packets
            var packet = (Packet)args.Payload;

            if (packet.Total == 1)
            {
                Received?.Invoke(this, new MessageReceivedEventArgs(packet.Sender, packet.Total, packet.Message, args.Received, args.Received));
                return;
            }

            var key = new MessageKey(packet.Sender, packet.Sequence, packet.Total);
            var receivedContext = _receivedContexts.GetValueOrAdd(key, _ => new ReceivedContext(packet.Total));

            receivedContext.Chunks.Add(packet.Message);

            if (receivedContext.Chunks.Count == 1)
            {
                receivedContext.FirstSent = args.Received;

                return;
            }

            if (receivedContext.Chunks.Count != packet.Total)
                return;

            Received?.Invoke(this, new MessageReceivedEventArgs(packet.Sender, packet.Total, receivedContext.Chunks.Concat(), receivedContext.FirstSent, args.Received));
            _receivedContexts.Remove(key);
        }

        #region Nested types

        public sealed class ReceivedContext
        {
            public List<string> Chunks { get; }
            public DateTime FirstSent { get; set; }

            public ReceivedContext(byte total)
            {
                Chunks = new List<string>(total);
            }

            public override string ToString() => $"[Chunks: {Chunks.Join(";")}, FirstSent: {FirstSent}]";
        }

        public sealed class SentContext
        {
            public DateTime FirstSent { get; set; }
            public byte Count { get; set; }
            public byte Total { get; }
            public byte[] RetransmissionsCounts { get; }

            public SentContext(byte total)
            {
                Total = total;
                RetransmissionsCounts = new byte[total];
            }

            public override string ToString() => $"[FirstSent: {FirstSent}, Count: {Count}, Total: {Total}, RetransmissionsCounts: {RetransmissionsCounts.Join(";")}]";
        }

        public sealed class MessageKey : IEquatable<MessageKey>
        {
            private readonly Address _sender;
            private readonly byte _sequence;
            private readonly byte _total;

            public MessageKey(Address sender, byte sequence, byte total)
            {
                _sequence = sequence;
                _total = total;
                _sender = sender;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = _sender?.GetHashCode() ?? 0;
                    hashCode = (hashCode * 397) ^ _sequence.GetHashCode();
                    hashCode = (hashCode * 397) ^ _total.GetHashCode();
                    return hashCode;
                }
            }

            public override string ToString() => $"[Sender: {_sender}, Sequence: {_sequence}, Total: {_total}]";
            public bool Equals(MessageKey other) => Equals(_sender, other._sender) && _sequence == other._sequence && _total == other._total;
            public override bool Equals(object obj) => !ReferenceEquals(null, obj) && obj is MessageKey && Equals((MessageKey)obj);
            public static bool operator ==(MessageKey left, MessageKey right) => left.Equals(right);
            public static bool operator !=(MessageKey left, MessageKey right) => !left.Equals(right);
        }

        #endregion
    }
}