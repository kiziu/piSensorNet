using System;
using System.Linq;
using System.Linq.Expressions;
using piSensorNet.Common.Enums;
using piSensorNet.Common.System;

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

        public static IOrderedQueryable<TSource> OrderBy<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, SortingDirectionEnum direction)
        {
            switch (direction)
            {
                case SortingDirectionEnum.Ascending:
                    return source.OrderBy(keySelector);

                case SortingDirectionEnum.Descending:
                    return source.OrderByDescending(keySelector);
                    
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        public static IOrderedQueryable<TSource> ThenBy<TSource, TKey>(this IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, SortingDirectionEnum direction)
        {
            if (Reflector.Instance<TKey>.Type == Reflector.Instance<object>.Type)
                return InternalSort(source, keySelector, direction);

            switch (direction)
            {
                case SortingDirectionEnum.Ascending:
                    return source.ThenBy(keySelector);

                case SortingDirectionEnum.Descending:
                    return source.ThenBy(keySelector);
                    
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        private static IOrderedQueryable<TSource> InternalSort<TSource, TKey>(IOrderedQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, SortingDirectionEnum direction)
        {
            var methodName = "ThenBy" + (direction == SortingDirectionEnum.Descending ? "Descending" : String.Empty);
            var unary = (UnaryExpression)keySelector.Body;
            if (unary.NodeType != ExpressionType.Convert)
                throw new ArgumentException("Wrong node type", nameof(keySelector));

            var member = (MemberExpression)unary.Operand;
            var memberType = member.Member.GetMemberType();
            var lambda = Expression.Lambda(member, keySelector.Parameters);
            var sort = Expression.Call(typeof(Queryable), methodName,
                new[] {source.ElementType, memberType}, 
                source.Expression, lambda);

            return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(sort);
        }
    }
}
