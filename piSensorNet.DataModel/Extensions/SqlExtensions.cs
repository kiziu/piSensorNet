using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using piSensorNet.Common.Extensions;

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
            => value.ToString(@"hh\:mm\:ss\.fff");
        
        public static string ToSql(this string value)
            => value != null ? $"'{value}'" : Null;

        internal static MethodInfo GetMethod(Type type)
        {
            var methods = typeof(SqlExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static);
            var method = methods.Where(i => i.GetParameters(), i => i.Length == 1 && i[0].ParameterType == type).Single();

            return method;
        }
            
    }
}
