using System;
using System.Linq;
using System.Text;
using piSensorNet.Common.Extensions;

namespace piSensorNet.Radio.NrfNet
{
    // LSB is sent first
    public class Address : IEquatable<Address>
    {
        internal byte[] Bytes { get; }

        public byte LeastSignificantByte => Bytes[0];

        internal Address()
        {
            Bytes = new byte[Nrf.AddressSize];
        }

        internal Address(byte[] bytes)
            : this()
        {
            if (bytes.Length != Nrf.AddressSize)
                throw new ArgumentException("Input array must have 5 elements", nameof(bytes));

            bytes.CopyTo(Bytes, 0);
        }

        public Address(byte[] bytes, byte leastSignificantByte)
            : this()
        {
            if (bytes.Length != Nrf.AddressSize)
                throw new ArgumentException("Input array must have 5 elements", nameof(bytes));

            bytes.CopyTo(Bytes, 0);

            Bytes[0] = leastSignificantByte;
        }

        public Address(Address address, byte leastSignificantByte)
            : this()
        {
            address.Bytes.CopyTo(Bytes, 0);

            Bytes[0] = leastSignificantByte;
        }

        public Address(string address)
        {
            if (address.Length != Nrf.AddressSize)
                throw new ArgumentException("String must have 5 characters", nameof(address));

            Bytes = new byte[Nrf.AddressSize];

            for (var i = 0; i < Nrf.AddressSize; ++i)
                Bytes[i] = (byte)address[Nrf.AddressSize - i - 1];
        }

        internal void SetLeastSignificantByte(byte value)
            => Bytes[0] = value;

        public bool Equals(Address other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (ReferenceEquals(other, null))
                return false;

            if (Bytes == null || other.Bytes == null || other.Bytes.Length != Bytes.Length)
                return false;

            for (var i = 0; i < Bytes.Length; ++i)
                if (other.Bytes[i] != Bytes[i])
                    return false;

            return true;
        }

        public override string ToString()
            => BitConverter.ToString(Bytes) + " (" + Encoding.ASCII.GetString(Bytes.Reverse()) + ")";

        public static implicit operator byte[](Address address)
        {
            var buffer = new byte[Nrf.AddressSize];

            address.Bytes.CopyTo(buffer, 0);

            return buffer;
        }

        public static implicit operator Address(byte[] bytes)
            => new Address(bytes);

        public static bool operator ==(Address left, Address right)
            => !ReferenceEquals(left, null) && left.Equals(right);

        public static bool operator !=(Address left, Address right)
            => !(left == right);

        public override bool Equals(object obj)
            => Equals(obj as Address);

        public override int GetHashCode()
            => Bytes?.GetHashCode() ?? 0;
    }
}