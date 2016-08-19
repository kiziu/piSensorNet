using System;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using piSensorNet.Common.Extensions;

namespace piSensorNet.Common.System
{
    public static class Reflector
    {
        [NotNull]
        private static MethodInfo GetMethod([NotNull] LambdaExpression e)
        {
            return ((MethodInfo)((ConstantExpression)((MethodCallExpression)((UnaryExpression)e.Body).Operand).Object).Value);
        }

        [NotNull]
        private static PropertyInfo GetProperty([NotNull] LambdaExpression e)
        {
            return (PropertyInfo)((MemberExpression)e.Body).Member;
        }

        [NotNull]
        private static FieldInfo GetField([NotNull] LambdaExpression e)
        {
            return (FieldInfo)((MemberExpression)e.Body).Member;
        }
        
        public static class Static
        {
            [NotNull]
            public static PropertyInfo Property<TReturn>([NotNull] Expression<Func<TReturn>> e)
            {
                return GetProperty(e);
            }

            [NotNull]
            public static FieldInfo Field<TReturn>([NotNull] Expression<Func<TReturn>> e)
            {
                return GetField(e);
            }

            [NotNull]
            public static MethodInfo Method([NotNull] Expression<Func<Action>> e)
            {
                return GetMethod(e);
            }

            [NotNull]
            public static MethodInfo Method<T1>([NotNull] Expression<Func<Action<T1>>> e)
            {
                return GetMethod(e);
            }

            [NotNull]
            public static MethodInfo Method<T1, T2>([NotNull] Expression<Func<Action<T1, T2>>> e)
            {
                return GetMethod(e);
            }

            [NotNull]
            public static MethodInfo Method<TReturn>([NotNull] Expression<Func<Func<TReturn>>> e)
            {
                return GetMethod(e);
            }

            [NotNull]
            public static MethodInfo Method<T1, TReturn>([NotNull] Expression<Func<Func<T1, TReturn>>> e)
            {
                return GetMethod(e);
            }

            [NotNull]
            public static MethodInfo Method<T1, T2, TReturn>([NotNull] Expression<Func<Func<T1, T2, TReturn>>> e)
            {
                return GetMethod(e);
            }
        }

        public static class Instance<T>
        {
            [NotNull]
            public static Type Type { get; } = typeof(T);

            // ReSharper disable once StaticMemberInGenericType
            [NotNull]
            public static string Name { get; } = Type.GetProperName();

            // ReSharper disable once StaticMemberInGenericType
            [NotNull]
            public static string EnumName { get; } = Type.Name.RemoveEnd(Instance<Enum>.Name);

            [NotNull]
            public static PropertyInfo Property<TReturn>([NotNull] Expression<Func<T, TReturn>> e)
            {
                return GetProperty(e);
            }

            [NotNull]
            public static FieldInfo Field<TReturn>([NotNull] Expression<Func<T, TReturn>> e)
            {
                return GetField(e);
            }

            [NotNull]
            public static MethodInfo Method([NotNull] Expression<Func<T, Action>> e)
            {
                return GetMethod(e);
            }

            [NotNull]
            public static MethodInfo Method<T1>([NotNull] Expression<Func<T, Action<T1>>> e)
            {
                return GetMethod(e);
            }

            [NotNull]
            public static MethodInfo Method<T1, T2>([NotNull] Expression<Func<T, Action<T1, T2>>> e)
            {
                return GetMethod(e);
            }

            [NotNull]
            public static MethodInfo Method<TReturn>([NotNull] Expression<Func<T, Func<TReturn>>> e)
            {
                return GetMethod(e);
            }

            [NotNull]
            public static MethodInfo Method<T1, TReturn>([NotNull] Expression<Func<T, Func<T1, TReturn>>> e)
            {
                return GetMethod(e);
            }

            [NotNull]
            public static MethodInfo Method<T1, T2, TReturn>([NotNull] Expression<Func<T, Func<T1, T2, TReturn>>> e)
            {
                return GetMethod(e);
            }
        }

        public static class LooselyTyped
        {
            
        }
    }
}