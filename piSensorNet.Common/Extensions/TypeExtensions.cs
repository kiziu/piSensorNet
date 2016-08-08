using System;
using System.Linq;
using JetBrains.Annotations;

namespace piSensorNet.Common.Extensions
{
    public static class TypeExtensions
    {
        public static bool IsNullable([NotNull] this Type type)
        {
            var nullable = GetNullable(type);

            return nullable != null;
        }

        [CanBeNull]
        public static Type GetNullable([NotNull] this Type type)
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
        
        [NotNull]
        public static string GetSignature<TDelegate>([NotNull] string methodName)
        {
            var type = typeof(TDelegate);

            return GetSignature(type, methodName);
        }

        [NotNull]
        public static string GetSignature([NotNull] Type delegateType, [NotNull] string methodName)
        {
            var method = delegateType.GetMethod("Invoke");
            var parameters = method.GetParameters().Select(i => $"{i.ParameterType.GetProperName()} {i.Name}").Join(", ");
            var returnType = method.ReturnType == typeof(void) ? "void" : method.ReturnType.GetProperName();

            return $"{returnType} {methodName}({parameters})";
        }
    }
}
