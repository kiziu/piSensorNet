﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using piSensorNet.Common.Custom;
using piSensorNet.Common.Custom.Interfaces;

namespace piSensorNet.Common.Extensions
{
    public static class CollectionExtensions
    {
        public static void Each<TElement>([NotNull] this IEnumerable<TElement> items, [InstantHandle] [NotNull] Action<TElement> action)
        {
            foreach (var item in items)
                action(item);
        }

        public static void Each<TElement>([NotNull] this IEnumerable<TElement> items, [InstantHandle] [NotNull] Action<int, TElement> action)
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
        public static HashSet<TElement> ToHashSet<TElement>([NotNull] this IEnumerable<TElement> source)
            => new HashSet<TElement>(source);

        [NotNull]
        public static Map<TForwardKey, TReverseKey> ToMap<TElement, TForwardKey, TReverseKey>([NotNull] this IEnumerable<TElement> source, [InstantHandle] [NotNull] Func<TElement, TForwardKey> forwardKeySelector, [InstantHandle] [NotNull] Func<TElement, TReverseKey> reverseKeySelector)
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
        public static Dictionary<TKey, TElementList> ToDictionary<TKey, TElement, TElementList>([NotNull] this IEnumerable<IGrouping<TKey, TElement>> items, [InstantHandle] [NotNull] Func<List<TElement>, TElementList> listModificator)
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
        public static IReadOnlyCollection<TElement> ReadOnly<TElement>([CanBeNull] this IReadOnlyCollection<TElement> list)
            => list;

        [CanBeNull]
        public static IReadOnlyMap<TForwardKey, TReverseKey> ReadOnly<TForwardKey, TReverseKey>([CanBeNull] this IReadOnlyMap<TForwardKey, TReverseKey> map)
            => map;

        [NotNull]
        public static List<TResultElement> Map<TSourceElement, TResultElement>([NotNull] this IReadOnlyCollection<TSourceElement> source, [InstantHandle] [NotNull] Func<TSourceElement, TResultElement> mappingFunction)
        {
            var result = new List<TResultElement>(source.Count);

            foreach (var item in source)
                result.Add(mappingFunction(item));

            return result;
        }
    }
}