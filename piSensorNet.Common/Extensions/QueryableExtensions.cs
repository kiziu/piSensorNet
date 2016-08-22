using System;
using System.Linq;
using System.Linq.Expressions;
using piSensorNet.Common.Enums;
using piSensorNet.Common.System;

namespace piSensorNet.Common.Extensions
{
    public static class QueryableExtensions
    {
        public static TElement? SingleOrNullable<TElement>(this IQueryable<TElement> source)
            where TElement : struct
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

        public static TElement? FirstOrNullable<TElement>(this IQueryable<TElement> source)
            where TElement : struct
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

        public static IOrderedQueryable<TElement> OrderBy<TElement, TKey>(this IQueryable<TElement> source, Expression<Func<TElement, TKey>> keySelector, SortingDirectionEnum direction)
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

        public static IOrderedQueryable<TElement> ThenBy<TElement, TKey>(this IOrderedQueryable<TElement> source, Expression<Func<TElement, TKey>> keySelector, SortingDirectionEnum direction)
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

        public static IOrderedQueryable<TElement> ThenBy<TElement>(this IOrderedQueryable<TElement> source, string propertyName, SortingDirectionEnum direction)
        {
            var keySelector = ExpressionExtensions.Create<TElement>(propertyName);

            return ThenBy(source, keySelector, direction);
        }

        private static IOrderedQueryable<TElement> InternalSort<TElement, TKey>(IOrderedQueryable<TElement> source, Expression<Func<TElement, TKey>> keySelector, SortingDirectionEnum direction)
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

            return (IOrderedQueryable<TElement>)source.Provider.CreateQuery<TElement>(sort);
        }
    }
}
