using System;
using System.Linq;

namespace piSensorNet.Common.Extensions
{
    public static class QueryableExtensions
    {
        public static T? SingleOrNullable<T>(this IQueryable<T> source)
            where T : struct
        {
            var result = source.ToList();

            switch (result.Count)
            {
                case 0:
                    return null;

                case 1:
                    return result[0];

                default:
                    throw new InvalidOperationException("Sequence contains more than one element");
            }
        }

        public static T? FirstOrNullable<T>(this IQueryable<T> source)
            where T : struct
        {
            var result = source.ToList();

            switch (result.Count)
            {
                case 0:
                    return null;

                default:
                    return result[0];
            }
        }
    }
}
