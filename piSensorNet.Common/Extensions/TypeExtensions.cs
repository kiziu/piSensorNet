using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace piSensorNet.Common.Extensions
{
    public static class TypeExtensions
    {
        public static bool IsNullable([NotNull] this Type type) 
            => GetNullable(type) != null;

        [CanBeNull]
        public static Type GetNullable([NotNull] this Type type) 
            => Nullable.GetUnderlyingType(type);

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
        public static MethodInfo GetDelegateMethod([NotNull] Type delegateType)
            => delegateType.GetMethod("Invoke");

        [NotNull]
        public static string GetDelegateSignature([NotNull] Type delegateType, [NotNull] string methodName)
        {
            var method = GetDelegateMethod(delegateType);
            var parameters = method.GetParameters().Select(i => $"{i.ParameterType.GetProperName()} {i.Name}").Join(", ");
            var returnType = method.ReturnType == typeof(void) ? "void" : method.ReturnType.GetProperName();

            return $"{returnType} {methodName}({parameters})";
        }

        [NotNull]
        public static string GetDelegateSignature<TDelegate>([NotNull] string methodName) 
            => GetDelegateSignature(typeof(TDelegate), methodName);
    }
}
