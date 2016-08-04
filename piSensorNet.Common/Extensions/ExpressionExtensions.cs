using System;
using System.Linq;
using System.Linq.Expressions;

namespace piSensorNet.Common.Extensions
{
    public static class ExpressionExtensions
    {
        public static string GetMemberName<TClass>(this Expression<Func<TClass, object>> expression)
            => InternalGetMemberName(expression);

        public static string GetMemberName<TClass, TProperty>(this Expression<Func<TClass, TProperty>> expression)
            => InternalGetMemberName(expression);

        private static string InternalGetMemberName<TClass, TProperty>(Expression<Func<TClass, TProperty>> expression)
        {
            var memberExpression = ExtractMemberExpression(expression);

            return memberExpression.Member.Name;
        }

        public static MemberExpression ExtractMemberExpression<TClass, TProperty>(this Expression<Func<TClass, TProperty>> expression)
        {
            var body = expression.Body;

            var memberExpression = body as MemberExpression;
            if (memberExpression != null)
                return memberExpression;

            var unaryExpression = body as UnaryExpression;
            if (unaryExpression == null)
                return null;

            memberExpression = unaryExpression.Operand as MemberExpression;
            if (memberExpression != null && (unaryExpression.NodeType == ExpressionType.Convert || unaryExpression.NodeType == ExpressionType.ConvertChecked))
                return memberExpression;

            return null;
        }
    }
}
