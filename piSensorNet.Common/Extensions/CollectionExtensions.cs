using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using piSensorNet.Common.Custom;
using piSensorNet.Common.Custom.Interfaces;

namespace piSensorNet.Common.Extensions
{
    public static class CollectionExtensions
    {
        public static void Each<TElement>([NotNull] this IEnumerable<TElement> items, [InstantHandle][NotNull] Action<TElement> action)
        {
            foreach (var item in items)
                action(item);
        }

        public static void Each<TElement, TResult>([NotNull] this IEnumerable<TElement> items, [InstantHandle][NotNull] Func<TElement, TResult> action)
        {
            foreach (var item in items)
                action(item);
        }

        public static void Each<TElement>([NotNull] this IEnumerable<TElement> items, [InstantHandle][NotNull] Action<int, TElement> action)
        {
            var index = 0;
            foreach (var item in items)
                action(index++, item);
        }

        [NotNull]
        public static IEnumerable<TElement> Where<TElement, TMember>([NotNull] this IEnumerable<TElement> items, [NotNull] Func<TElement, TMember> memberSelector, [NotNull] Func<TMember, bool> memberPredicate)
            => items.Select(i => new
                                 {
                                     item = i,
                                     member = memberSelector(i)
                                 }).
                     Where(i => memberPredicate(i.member))
                    .Select(i => i.item);

        [NotNull]
        public static IEnumerable<TElement> Recursive<TElement>([NotNull] this TElement item, [NotNull] Func<TElement, TElement> memberSelector, [NotNull] Func<TElement, bool> stopCondition)
        {
            while (true)
            {
                if (stopCondition(item))
                    yield break;

                yield return item;

                item = memberSelector(item);
            }
        }

        [NotNull]
        public static HashSet<TElement> ToHashSet<TElement>([NotNull] this IEnumerable<TElement> source)
            => new HashSet<TElement>(source);

        [NotNull]
        public static Map<TForwardKey, TReverseKey> ToMap<TElement, TForwardKey, TReverseKey>([NotNull] this IEnumerable<TElement> source, [InstantHandle][NotNull] Func<TElement, TForwardKey> forwardKeySelector, [InstantHandle][NotNull] Func<TElement, TReverseKey> reverseKeySelector)
        {
            var capacity = 0;
            var collection = source as ICollection<TElement>;
            if (collection != null)
                capacity = collection.Count;
            else
            {
                var readOnlyCollection = source as IReadOnlyCollection<TElement>;
                if (readOnlyCollection != null)
                    capacity = readOnlyCollection.Count;
            }

            return Custom.Map.Create(source, forwardKeySelector, reverseKeySelector, capacity);
        }

        [NotNull]
        public static GroupedList<TKey, TElement> ToGroupedList<TKey, TElement>([NotNull] this IEnumerable<TElement> items, TKey key)
            => new GroupedList<TKey, TElement>(items, key);

        [NotNull]
        public static Dictionary<TKey, List<TElement>> ToDictionary<TKey, TElement>([NotNull] this IEnumerable<IGrouping<TKey, TElement>> items)
            => items.ToDictionary(i => i.Key, i => i.ToList());

        [NotNull]
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>([NotNull] this IEnumerable<KeyValuePair<TKey, TValue>> items)
            => items.ToDictionary(i => i.Key, i => i.Value);

        [NotNull]
        public static Dictionary<TKey, TElementList> ToDictionary<TKey, TElement, TElementList>([NotNull] this IEnumerable<IGrouping<TKey, TElement>> items, [InstantHandle][NotNull] Func<List<TElement>, TElementList> listModificator)
            => items.ToDictionary(i => i.Key, i => listModificator(i.ToList()));

        [NotNull]
        public static TCollection Add<TElement, TCollection>([NotNull] this TCollection collection, [NotNull] IEnumerable<TElement> items)
            where TCollection : ICollection<TElement>
        {
            foreach (var item in items)
                collection.Add(item);

            return collection;
        }

        [CanBeNull]
        [ContractAnnotation("list:notnull => notnull; list:null => null")]
        public static IReadOnlyCollection<TElement> ReadOnly<TElement>([CanBeNull] this IReadOnlyCollection<TElement> list)
            => list;

        [CanBeNull]
        public static IReadOnlyMap<TForwardKey, TReverseKey> ReadOnly<TForwardKey, TReverseKey>([CanBeNull] this IReadOnlyMap<TForwardKey, TReverseKey> map)
            => map;

        [NotNull]
        public static List<TResultElement> Map<TSourceElement, TResultElement>([NotNull] this IReadOnlyCollection<TSourceElement> source, [InstantHandle][NotNull] Func<TSourceElement, TResultElement> mappingFunction)
            => InternalMapList(source, source.Count, mappingFunction);

        [NotNull]
        public static List<TResultElement> Map<TSourceElement, TResultElement>([NotNull] this IList<TSourceElement> source, [InstantHandle][NotNull] Func<TSourceElement, TResultElement> mappingFunction)
            => InternalMapList(source, source.Count, mappingFunction);

        [NotNull]
        public static List<TResultElement> Map<TSourceElement, TResultElement>([NotNull] this List<TSourceElement> source, [InstantHandle][NotNull] Func<TSourceElement, TResultElement> mappingFunction)
            => source.ReadOnly().Map(mappingFunction);

        [NotNull]
        public static TResultElement[] MapArray<TSourceElement, TResultElement>([NotNull] this IReadOnlyCollection<TSourceElement> source, [InstantHandle][NotNull] Func<TSourceElement, TResultElement> mappingFunction)
            => InternalMapArray(source, source.Count, mappingFunction);

        [NotNull]
        public static TResultElement[] MapArray<TSourceElement, TResultElement>([NotNull] this ICollection<TSourceElement> source, [InstantHandle][NotNull] Func<TSourceElement, TResultElement> mappingFunction)
            => InternalMapArray(source, source.Count, mappingFunction);

        [NotNull]
        public static TResultElement[] MapArray<TSourceElement, TResultElement>([NotNull] this TSourceElement[] source, [InstantHandle][NotNull] Func<TSourceElement, TResultElement> mappingFunction)
            => InternalMapArray(source, source.Length, mappingFunction);

        [NotNull]
        private static List<TResultElement> InternalMapList<TSourceElement, TResultElement>([NotNull] IEnumerable<TSourceElement> source, int count, [NotNull] Func<TSourceElement, TResultElement> mappingFunction)
        {
            var result = new List<TResultElement>(count);

            foreach (var item in source)
                result.Add(mappingFunction(item));

            return result;
        }

        [NotNull]
        private static TResultElement[] InternalMapArray<TSourceElement, TResultElement>([NotNull] IEnumerable<TSourceElement> source, int count, [NotNull] Func<TSourceElement, TResultElement> mappingFunction)
        {
            var result = new TResultElement[count];

            var i = 0;
            foreach (var item in source)
                result[i++] = mappingFunction(item);

            return result;
        }

        [NotNull]
        public static T[] Zero<T>([NotNull] this T[] array)
        {
            for (var i = 0; i < array.Length; ++i)
                array[i] = default(T);

            return array;
        }

        [NotNull]
        public static T[] Reverse<T>([NotNull] this T[] array)
        {
            var length = array.Length;

            var result = new T[length];

            for (var i = 0; i < length; ++i)
                result[i] = array[length - i - 1];

            return result;
        }

        public static int GetArrayHashCode<T>([ItemNotNull][NotNull] this T[] array)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));

            var hashcode = 17;

            foreach (var item in array)
                hashcode = hashcode * 31 + item.GetHashCode();

            return hashcode;
        }
    }
}