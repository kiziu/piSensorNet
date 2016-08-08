using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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

        public static IReadOnlyDictionary<PropertyInfo, object> InternalExtractPropertiesFromEqualityComparisons(Expression e)
        {
            var properties = new Dictionary<PropertyInfo, object>();

            InternalExtractPropertiesFromEqualityComparisons(e, properties);

            return properties;
        }

        [SuppressMessage("ReSharper", "TailRecursiveCall")]
        private static void InternalExtractPropertiesFromEqualityComparisons(Expression e, Dictionary<PropertyInfo, object> properties)
        {
            var binary = e as BinaryExpression;
            if (binary == null)
                return;

            var property = binary.Left as MemberExpression;
            if (property == null)
            {
                var unary = binary.Left as UnaryExpression;
                if (unary != null && unary.NodeType == ExpressionType.Convert)
                    property = unary.Operand as MemberExpression;
            }

            if (property != null && e.NodeType == ExpressionType.Equal)
            {
                var value = binary.Right;

                var unary = value as UnaryExpression;
                if (unary != null && unary.NodeType == ExpressionType.Convert)
                    value = unary.Operand;

                var constantValue = value as ConstantExpression;
                if (constantValue != null)
                {
                    properties.Add((PropertyInfo)property.Member, constantValue.Value);
                    return;
                }

                var member = value as MemberExpression;
                if (member != null)
                {
                    var valueGetter = Expression.Lambda<Func<object>>(Expression.Convert(member, typeof(object))).Compile();

                    properties.Add((PropertyInfo)property.Member, valueGetter());
                    // ReSharper disable once RedundantJumpStatement
                    return;
                }
            }
            else if (e.NodeType == ExpressionType.AndAlso)
            {
                InternalExtractPropertiesFromEqualityComparisons(binary.Left, properties);
                InternalExtractPropertiesFromEqualityComparisons(binary.Right, properties);
            }
        }
    }
}
