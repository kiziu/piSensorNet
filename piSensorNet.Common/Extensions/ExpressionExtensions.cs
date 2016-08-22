using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using piSensorNet.Common.System;

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

        public static Expression<Func<TClass, TProperty>> Create<TClass, TProperty>(string memberName)
        {
            var parameter = Expression.Parameter(Reflector.Instance<TClass>.Type, "e");
            var member = Expression.PropertyOrField(parameter, memberName);
            var body = (Expression)member;

            if (Reflector.Instance<TProperty>.Type == Reflector.Instance<object>.Type
                && member.Member.GetMemberType() != Reflector.Instance<object>.Type)
                body = Expression.Convert(body, Reflector.Instance<object>.Type);

            var expression = Expression.Lambda<Func<TClass, TProperty>>(body, parameter);

            return expression;
        }


        public static Expression<Func<TClass, object>> Create<TClass>(string memberName) 
            => Create<TClass, object>(memberName);

        public static IReadOnlyDictionary<PropertyInfo, object> ExtractPropertiesFromEqualityComparisons(Expression e)
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

        public static StringBuilder ExtractWhereClauseFromPredicate(Expression e, StringBuilder builder, IReadOnlyDictionary<Type, Func<object, string>> formatters)
        {
            var binary = e as BinaryExpression;
            if (binary == null)
            {
                var methodCall = e as MethodCallExpression;
                if (methodCall == null)
                    return builder;

                if (methodCall.Method.Name != "Contains")
                    throw new NotSupportedException($"Method '{methodCall.Method.Name}' is not supported;");

                if (methodCall.Arguments.Count != 2 || methodCall.Arguments[1].Type != Reflector.Instance<int>.Type)
                    throw new NotSupportedException($"Arguments [{methodCall.Arguments.Select(i => $"{i.Type}").Join(", ")}] are not supported.");

                var propertyArgument = (MemberExpression)methodCall.Arguments[1];
                var formatter = formatters[propertyArgument.Member.GetMemberType()];
                var valuesSelector = Expression.Lambda<Func<IEnumerable>>(methodCall.Arguments[0]).Compile();
                var values = valuesSelector().Cast<object>().Select(formatter).Join(", ");

                builder.Append($"`{propertyArgument.Member.Name}` IN ({values})");

                return builder;
            }

            var property = binary.Left as MemberExpression;
            if (property == null)
            {
                var unary = binary.Left as UnaryExpression;
                if (unary != null && unary.NodeType == ExpressionType.Convert)
                    property = unary.Operand as MemberExpression;
            }

            if (property != null)
            {
                var value = binary.Right;

                var unary = value as UnaryExpression;
                if (unary != null && unary.NodeType == ExpressionType.Convert)
                    value = unary.Operand;

                var propertyName = property.Member.Name;
                var nestedProperty = property.Expression as MemberExpression;
                if (nestedProperty != null)
                    throw new NotSupportedException($"Nested '{Reflector.Instance<MemberExpression>.Name}' in '{e}' not supported.");

                string op;
                switch (e.NodeType)
                {
                    case ExpressionType.Equal:
                        op = "=";
                        break;

                    case ExpressionType.NotEqual:
                        op = "!=";
                        break;

                    case ExpressionType.GreaterThan:
                        op = ">";
                        break;

                    case ExpressionType.GreaterThanOrEqual:
                        op = ">=";
                        break;

                    case ExpressionType.LessThan:
                        op = "<";
                        break;

                    case ExpressionType.LessThanOrEqual:
                        op = "<=";
                        break;

                    default:
                        throw new NotSupportedException($"Operand '{e.NodeType}' is not supported yet.");
                }

                var constantValue = value as ConstantExpression;
                if (constantValue != null)
                {
                    builder.Append($"`{propertyName}` {op} {formatters[constantValue.Type](constantValue.Value)}");
                    return builder;
                }

                var member = value as MemberExpression;
                if (member != null)
                {
                    var valueGetter = Expression.Lambda<Func<object>>(Expression.Convert(member, typeof(object))).Compile();

                    builder.Append($"`{propertyName}` {op} {formatters[member.Member.GetMemberType()](valueGetter())}");
                    // ReSharper disable once RedundantJumpStatement
                    return builder;
                }
            }
            else if (e.NodeType == ExpressionType.AndAlso || e.NodeType == ExpressionType.OrElse)
            {
                builder.Append("(");

                ExtractWhereClauseFromPredicate(binary.Left, builder, formatters);

                builder.Append(e.NodeType == ExpressionType.AndAlso ? " AND " : " OR ");

                // ReSharper disable once TailRecursiveCall
                ExtractWhereClauseFromPredicate(binary.Right, builder, formatters);

                builder.Append(")");
            }

            return builder;
        }
    }
}
