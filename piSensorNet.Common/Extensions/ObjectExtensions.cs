using System;
using System.Linq;
using JetBrains.Annotations;

namespace piSensorNet.Common.Extensions
{
    public static class ObjectExtensions
    {
        public static T Modify<T>(this T o, [InstantHandle] Action<T> action)
        {
            action(o);

            return o;
        }
    }
}