using System;
using System.Linq;
using JetBrains.Annotations;

namespace piSensorNet.Common.Extensions
{
    public static class TypeExtensions
    {
        public static bool IsNullable(this Type type)
        {
            var nullable = GetNullable(type);

            return nullable != null;
        }

        public static Type GetNullable(this Type type)
        {
            return Nullable.GetUnderlyingType(type);
        }

        [NotNull]
        public static string GetProperName([NotNull] this Type type, bool includeGenericArguments = true)
        {
            if (!type.IsGenericType)
                return type.Name;

            var typeName = type.Name.SubstringBefore('`') ?? type.Name;
            if (!includeGenericArguments)
                return typeName;

            var genericArguments = type.GetGenericArguments().Select(i => i.Name).Join(", ");

            var name = typeName + "<" + genericArguments + ">";

            return name;
        }
    }
}
