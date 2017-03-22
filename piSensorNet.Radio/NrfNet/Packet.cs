using System;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace piSensorNet.Radio.NrfNet
{
    public sealed class Packet
    {
        public static Address BaseAddress { get; set; }

        public const int HeaderSize = 4;
        public const int MessageSize = Nrf.PayloadSize - HeaderSize;

        public byte Sequence { get; }
        public Address Sender { get; }
        public byte Current { get; }
        public byte Total { get; }
        public string Message { get; }

        public Packet(byte sequence, [NotNull] Address sender, byte current, byte total, [NotNull] string message)
        {
            if (sender == null) throw new ArgumentNullException(nameof(sender));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (message.Length > MessageSize) throw new ArgumentException($"Message size {message.Length} is greater than maximum of {MessageSize}.", nameof(message));

            Sequence = sequence;
            Sender = sender;
            Current = current;
            Total = total;
            Message = message;
        }

        public override string ToString()
            => $"[Sequence: {Sequence}, Sender: {Sender}, Current: {Current}, Total: {Total}, Message: {Message}]";

        public static explicit operator byte[](Packet input)
        {
            var buffer = new byte[Nrf.PayloadSize];

            buffer[0] = input.Sender.LeastSignificantByte;
            buffer[1] = input.Sequence;
            buffer[2] = input.Total;
            buffer[3] = input.Current;

            var messageBytes = Encoding.ASCII.GetBytes(input.Message);

            Array.Copy(messageBytes, 0, buffer, HeaderSize, input.Message.Length);

            return buffer;
        }

        public static explicit operator Packet(byte[] input)
        {
            if (BaseAddress == null)
                throw new InvalidOperationException($"{nameof(BaseAddress)} must be set before casting to {nameof(Packet)}.");

            if (input.Length < HeaderSize)
                throw new ArgumentException($"Input array must have at least {HeaderSize} elements.", nameof(input));

            var message = Encoding.ASCII.GetString(input, HeaderSize, input.Length - HeaderSize);

            return new Packet(input[1], new Address(BaseAddress, input[0]), input[3], input[2], message);
        }
    }
}