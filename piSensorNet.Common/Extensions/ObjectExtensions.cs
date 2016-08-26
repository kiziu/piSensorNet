using System;
using System.Linq;
using JetBrains.Annotations;

namespace piSensorNet.Common.Extensions
{
    public static class ObjectExtensions
    {
        [CanBeNull]
        [ContractAnnotation("o:notnull => notnull; o:null => null")]
        public static TObject Modify<TObject>([CanBeNull] this TObject o, [InstantHandle] [NotNull] Action<TObject> action)
        {
            action(o);

            return o;
        }

        [NotNull]
        public static TObject If<TObject>([NotNull] this TObject o, [NotNull] Func<TObject, bool> condition, [InstantHandle] [NotNull] Action<TObject> action)
        {
            if (condition(o))
                action(o);

            return o;
        }

        [NotNull]
        public static TObject If<TObject>([NotNull] this TObject o, bool condition, [InstantHandle] [NotNull] Action<TObject> action)
            => If(o, i => condition, action);

        [NotNull]
        public static Type GetRealType([NotNull] this object obj)
        {
            var objType = obj.GetType();
            var underlyingType = objType.GetNullable();

            return underlyingType ?? objType;
        }

        [CanBeNull]
        public static TResult For<TObject, TResult>([CanBeNull] this TObject o, [NotNull] Func<TObject, TResult> selector)
            where TResult : class
            => o != null ? selector(o) : null;
    }
}