using System;
using System.Linq;

namespace piSensorNet.Common.Custom
{
    public class Null
    {
        public static Null Value { get; } = new Null();

        private Null() {}
    }
}