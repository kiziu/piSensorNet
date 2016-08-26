using System;
using System.Linq;
using JetBrains.Annotations;

namespace piSensorNet.Common.Custom
{
    public sealed class JsonLiteral
    {
        public const string Default = "null";

        private readonly string _inner;

        private JsonLiteral([CanBeNull] string inner)
        {
            _inner = inner;
        }

        public static implicit operator JsonLiteral([CanBeNull] string input)
        {
            return new JsonLiteral(input);
        }

        public static explicit operator string([CanBeNull] JsonLiteral input)
        {
            return input?.ToString();
        }

        public override string ToString()
        {
            return _inner ?? Default;
        }
    }
}
