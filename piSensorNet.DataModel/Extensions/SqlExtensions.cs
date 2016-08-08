using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using piSensorNet.Common.Extensions;
using piSensorNet.Common.System;

namespace piSensorNet.DataModel.Extensions
{
    public static class SqlExtensions
    {
        public const string Null = "NULL";

        public static string ToSql<T>(this T? value, Func<T, string> toString)
            where T : struct
            => value.HasValue ? toString(value.Value) : Null;

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

        internal static MethodInfo GetMethod(Type type)
        {
            var methods = typeof(SqlExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var method = methods.Where(i => i.GetParameters(), i => i.Length == 1 && i[0].ParameterType == type).Single();

            return method;
        }

        public static string ToSql(this object value, Type type)
        {
            type = type ?? value.GetType();
            var nullable = type.GetNullable();

            if (type == Reflector.Instance<string>.Type) return ToSql((string)value);

            if (type == Reflector.Instance<bool>.Type) return ToSql((bool)value);
            if (type == Reflector.Instance<bool?>.Type) return ToSql((bool?)value, ToSql);

            if (type == Reflector.Instance<int>.Type) return ToSql((int)value);
            if (type == Reflector.Instance<int?>.Type) return ToSql((int?)value, ToSql);

            if (type == Reflector.Instance<decimal>.Type) return ToSql((decimal)value);
            if (type == Reflector.Instance<decimal?>.Type) return ToSql((decimal?)value, ToSql);

            if (type == Reflector.Instance<DateTime>.Type) return ToSql((DateTime)value);
            if (type == Reflector.Instance<DateTime?>.Type) return ToSql((DateTime?)value, ToSql);

            if (type == Reflector.Instance<TimeSpan>.Type) return ToSql((TimeSpan)value);
            if (type == Reflector.Instance<TimeSpan?>.Type) return ToSql((TimeSpan?)value, ToSql);

            if (type.IsEnum || nullable != null && nullable.IsEnum) return ToSql((int)value);
            
            throw new NotSupportedException();
        }
    }
}
