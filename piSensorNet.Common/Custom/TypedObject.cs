using System;
using System.Linq;
using JetBrains.Annotations;

namespace piSensorNet.Common.Custom
{
    public sealed class TypedObject
    {
        public Type Type { get; }

        public object Value { get; }

        public TypedObject([CanBeNull] object value, [NotNull] Type type)
        {
            Value = value;
            Type = type;
        }

        public TypedObject([NotNull] object value)
            : this(value, value.GetType()) {}

        [NotNull]
        public static TypedObject Create([CanBeNull] object value, [NotNull] Type type)
            => new TypedObject(value, type);

        [NotNull]
        public static TypedObject Create([NotNull] object value)
            => new TypedObject(value);
    }

    public static class TypedObjectExtensions
    {
        [NotNull]
        public static TypedObject ToTyped([CanBeNull] this object value, [NotNull] Type type)
            => TypedObject.Create(value, type);

        [NotNull]
        public static TypedObject ToTyped([NotNull] this object value)
            => TypedObject.Create(value, value.GetType());
    }
}