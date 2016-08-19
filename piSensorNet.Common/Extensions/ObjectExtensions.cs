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


        [NotNull]
        public static Type GetRealType([NotNull] this object obj)
        {
            var objType = obj.GetType();
            var underlyingType = objType.GetNullable();

            return underlyingType ?? objType;
        }
    }
}