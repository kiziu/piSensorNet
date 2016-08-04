using System;
using System.Linq;

namespace piSensorNet.Common.Custom
{
    public struct Optional<T> : IEquatable<Optional<T>>
    {
        public static Optional<T> None { get; } = new Optional<T>(default(T));

        public bool HasValue { get; }

        public T Value
        {
            get
            {
                if (!HasValue)
                    throw new InvalidOperationException("Optional object must have a value.");

                return _value;
            }
        }

        private readonly T _value;

        public Optional(T value)
        {
            _value = value;
            HasValue = true;
        }

        public T GetValueOrDefault()
        {
            return _value;
        }

        public T GetValueOrDefault(T defaultValue)
        {
            if (defaultValue == null)
                throw new ArgumentNullException(nameof(defaultValue));

            return HasValue ? _value : defaultValue;
        }

        public bool Equals(Optional<T> other)
        {
            if (HasValue ^ other.HasValue)
                return false;

            if (!HasValue && !other.HasValue)
                return true;

            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return !HasValue;

            return obj is Optional<T> && Equals((Optional<T>)obj);
        }

        public override int GetHashCode()
        {
            if (!HasValue)
                return 0;

            return _value.GetHashCode();
        }

        public static implicit operator Optional<T>(T value)
        {
            return new Optional<T>(value);
        }

        public static explicit operator T(Optional<T> value)
        {
            return value.Value;
        }

        public static bool operator ==(Optional<T> left, Optional<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Optional<T> left, Optional<T> right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            if (!HasValue)
                return String.Empty;

            return _value.ToString();
        }
    }

    public static class Optional
    {
        private static bool IsNull<T>(T value)
        {
            return ReferenceEquals(value, null);
        }

        public static Optional<T> From<T>(T valueOrNull)
        {
            return IsNull(valueOrNull) ? Optional<T>.None : Some(valueOrNull);
        }

        public static Optional<T> Some<T>(T value)
        {
            if (IsNull(value))
                throw new ArgumentNullException(nameof(value));

            return new Optional<T>(value);
        }
    }
}
