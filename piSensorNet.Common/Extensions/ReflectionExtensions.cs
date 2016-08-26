using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using piSensorNet.Common.System;

namespace piSensorNet.Common.Extensions
{
    public static class ReflectionExtensions
    {
        [ContractAnnotation("throwOnWrongMemberType:true => notnull")]
        [CanBeNull]
        public static Type GetMemberType([NotNull] this MemberInfo memberInfo, bool throwOnWrongMemberType = true)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)memberInfo).FieldType;

                case MemberTypes.Property:
                    return ((PropertyInfo)memberInfo).PropertyType;

                default:
                    if (throwOnWrongMemberType)
                        throw new ArgumentException("Wrong member type.", nameof(memberInfo));

                    return null;
            }
        }

        [ContractAnnotation("throwOnWrongMemberType:true => notnull")]
        [CanBeNull]
        public static object GetMemberValue([NotNull] this MemberInfo memberInfo, object source, bool throwOnWrongMemberType = true)
        {
            switch (memberInfo.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)memberInfo).GetValue(source);

                case MemberTypes.Property:
                    return ((PropertyInfo)memberInfo).GetValue(source);

                default:
                    if (throwOnWrongMemberType)
                        throw new ArgumentException("Wrong member type.", nameof(memberInfo));

                    return null;
            }
        }

        [NotNull]
        public static IReadOnlyCollection<Type> GetImplementations<TBase>()
            where TBase : class
        {
            var baseType = Reflector.Instance<TBase>.Type;
            var types = baseType.Assembly
                                .GetTypes()
                                .Where(i => i.IsNotPublic && i.IsSealed && i.IsClass)
                                .Where(baseType.IsAssignableFrom)
                                .ToList();

            return types;
        }

        [NotNull]
        public static string GetFullName(this MethodBase method) 
            => $"{method.DeclaringType.FullName}.{method.Name}";
    }
}
