using System;
using System.Linq;
using JetBrains.Annotations;

namespace piSensorNet.Common.Extensions
{
    public static class ObjectExtensions
    {
        [NotNull]
        public static TObject For<TObject>([NotNull] this TObject o, [InstantHandle][NotNull] Action<TObject> action)
        {
            if (o == null) throw new ArgumentNullException(nameof(o));

            action(o);

            return o;
        }

        [NotNull]
        public static TObject If<TObject>([NotNull] this TObject o, [NotNull] Func<TObject, bool> condition, [NotNull][InstantHandle] Action<TObject> action)
        {
            if (o == null) throw new ArgumentNullException(nameof(o));
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            if (action == null) throw new ArgumentNullException(nameof(action));

            if (condition(o))
                action(o);

            return o;
        }

        [NotNull]
        public static TObject If<TObject>([NotNull] this TObject o, bool condition, [InstantHandle][NotNull] Action<TObject> action)
            => If(o, i => condition, action);

        [NotNull]
        public static Type GetRealType([NotNull] this object obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            var objType = obj.GetType();
            var underlyingType = objType.GetNullable();

            return underlyingType ?? objType;
        }

        [CanBeNull]
        public static TResult For<TObject, TResult>([CanBeNull] this TObject o, [NotNull] Func<TObject, TResult> selector)
            where TResult : class
            => o != null ? selector(o) : null;

        public static bool IsEqualTo<T>([NotNull] this T obj, params T[] values)
            => values.Any(i => obj.Equals(i));
    }
}