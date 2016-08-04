using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace piSensorNet.Common.Extensions
{
    public static class EnumerableExtensions
    {
        public static void Each<T>(this IEnumerable<T> items, [InstantHandle] Action<T> action)
        {
            foreach (var item in items)
                action(item);
        }

        public static IEnumerable<TItem> Where<TItem, TMember>(this IEnumerable<TItem> items, Func<TItem, TMember> memberSelector, Func<TMember, bool> memberPredicate)
        {
            return items.Select(i => new
                                        {
                                            item = i,
                                            member = memberSelector(i)
                                        }).
                         Where(i => memberPredicate(i.member))
                        .Select(i => i.item);
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        public static ICollection<T> Add<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
                collection.Add(item);

            return collection;
        }
    }
}
