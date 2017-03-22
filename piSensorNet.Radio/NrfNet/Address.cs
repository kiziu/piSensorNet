using System;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using piSensorNet.Common.Extensions;

namespace piSensorNet.Radio.NrfNet
{
    // LSB is sent first
    public sealed class Address : IEquatable<Address>
    {
        [NotNull]
        internal byte[] Bytes { get; }

        public byte LeastSignificantByte => Bytes[0];

        public string Readable => Encoding.ASCII.GetString(Bytes.Reverse());

        internal Address()
        {
            Bytes = new byte[Nrf.AddressSize];
        }

        internal Address([NotNull] byte[] bytes)
            : this()
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length != Nrf.AddressSize) throw new ArgumentException("Input array must have 5 elements", nameof(bytes));

            bytes.CopyTo(Bytes, 0);
        }

        public Address([NotNull] byte[] bytes, byte leastSignificantByte)
            : this(bytes)
        {
            Bytes[0] = leastSignificantByte;
        }

        public Address([NotNull] Address address, byte leastSignificantByte)
            : this()
        {
            if (address == null) throw new ArgumentNullException(nameof(address));

            address.Bytes.CopyTo(Bytes, 0);

            Bytes[0] = leastSignificantByte;
        }

        public Address([NotNull] string address)
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (address.Length != Nrf.AddressSize) throw new ArgumentException("String must have 5 characters", nameof(address));

            Bytes = new byte[Nrf.AddressSize];

            var addressBytes = Encoding.ASCII.GetBytes(address);
            for (var i = 0; i < Nrf.AddressSize; ++i)
                Bytes[i] = addressBytes[Nrf.AddressSize - i - 1];
        }

        public override string ToString()
            => $"[{Readable} ({Bytes.Select(i => i.ToString("X2")).Concat()})]";

        public bool Equals(Address other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (other.Bytes.Length != Bytes.Length)
                return false;

            for (var i = 0; i < Bytes.Length; ++i)
                if (other.Bytes[i] != Bytes[i])
                    return false;

            return true;
        }

        public override bool Equals(object other) 
            => Equals(other as Address);

        public static bool operator ==(Address left, Address right) 
            => Equals(left, right);

        public static bool operator !=(Address left, Address right)
            => !Equals(left, right);

        public override int GetHashCode()
            => Bytes.GetArrayHashCode();

        internal void SetLeastSignificantByte(byte value)
            => Bytes[0] = value;

        public static explicit operator byte[] (Address address)
        {
            var buffer = new byte[Nrf.AddressSize];

            address.Bytes.CopyTo(buffer, 0);

            return buffer;
        }

        public static explicit operator Address(byte[] bytes)
            => new Address(bytes);

        public static explicit operator Address(string input)
            => new Address(input);

        public static explicit operator string(Address input)
            => Encoding.ASCII.GetString(input.Bytes);
    }
}
 