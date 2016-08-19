using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using piSensorNet.Common.System;

namespace piSensorNet.DataModel.Extensions
{
    public static class SqlExtensions
    {
        public const string Null = "NULL";

        static SqlExtensions()
        {
            Methods = typeof(SqlExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                .Where(i => i.Name.Equals("ToSql") && i.GetParameters().Length == 1)
                                                .ToDictionary(i => i.GetParameters()[0].ParameterType, i => i);

            var formatters = new Dictionary<Type, Func<object, string>>(Methods.Count);
            foreach (var method in Methods)
            {
                var parameterType = method.Key;
                var methodInfo = method.Value;

                var parameter = Expression.Parameter(Reflector.Instance<object>.Type, "value");
                var converted = Expression.Convert(parameter, parameterType);
                var methodCall = Expression.Call(methodInfo, converted);
                var lambda = Expression.Lambda<Func<object, string>>(methodCall, parameter);

                formatters.Add(parameterType, lambda.Compile());

                if (parameterType.IsClass)
                    continue;

                var nullableType = typeof(Nullable<>).MakeGenericType(parameterType);
                converted = Expression.Convert(parameter, nullableType);
                var memberAccess = Expression.PropertyOrField(converted, "Value");
                methodCall = Expression.Call(methodInfo, memberAccess);
                var condition = Expression.Condition(
                    Expression.Equal(parameter, Expression.Constant(null, Reflector.Instance<object>.Type)),
                    Expression.Constant(Null),
                    methodCall);

                lambda = Expression.Lambda<Func<object, string>>(condition, parameter);

                formatters.Add(nullableType, lambda.Compile());
            }

            Formatters = formatters;
        }

        internal static IReadOnlyDictionary<Type, MethodInfo> Methods { get; }
        public static IReadOnlyDictionary<Type, Func<object, string>> Formatters { get; }

        public static string ToSql(this DateTime value)
            => $"'{value:yyyy-MM-dd HH:mm:ss.fff}'";

        public static string ToSql(this bool value)
            => value ? "true" : "false";

        public static string ToSql(this int value)
            => value.ToString("D", CultureInfo.InvariantCulture);

        public static string ToSql(this decimal value)
            => value.ToString("#.##############", CultureInfo.InvariantCulture);

        public static string ToSql(this Enum value)
            => value.ToString("D");

        public static string ToSql(this TimeSpan value)
            => $"'{value:hh':'mm':'ss'.'fff}'";
        
        public static string ToSql(this string value)
            => value != null ? $"'{value}'" : Null;

        public static string ToSql(this object value)
            => value == null ? Null : Formatters[value.GetType()](value);
    }
}
