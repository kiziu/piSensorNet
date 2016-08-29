using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using piSensorNet.Common.Extensions;

namespace piSensorNet.Common.System
{
    public static class Reflector
    {
        [DebuggerStepThrough]
        [NotNull]
        public static MethodInfo GetMethod([NotNull] LambdaExpression e)
            => ((MethodInfo)((ConstantExpression)((MethodCallExpression)((UnaryExpression)e.Body).Operand).Object).Value);

        [DebuggerStepThrough]
        [NotNull]
        public static MethodInfo GetIndexer([NotNull] LambdaExpression e)
            => ((MethodCallExpression)e.Body).Method;

        [DebuggerStepThrough]
        [NotNull]
        public static PropertyInfo GetProperty([NotNull] LambdaExpression e)
            => (PropertyInfo)((MemberExpression)e.Body).Member;

        [DebuggerStepThrough]
        [NotNull]
        public static FieldInfo GetField([NotNull] LambdaExpression e)
            => (FieldInfo)((MemberExpression)e.Body).Member;

        [DebuggerStepThrough]
        [CanBeNull]
        private static ConstructorInfo GetConstructor(IReadOnlyCollection<ConstructorInfo> constructors, params Type[] types)
        {
            return constructors.Where(i => i.GetParameters()
                                            .Select(ii => ii.ParameterType)
                                            .SequenceEqual(types))
                               .SingleOrDefault();
        }

        public static class Static
        {
            [DebuggerStepThrough]
            [NotNull]
            public static PropertyInfo Property<TReturn>([NotNull] Expression<Func<TReturn>> e)
                => GetProperty(e);

            [DebuggerStepThrough]
            [NotNull]
            public static FieldInfo Field<TReturn>([NotNull] Expression<Func<TReturn>> e)
                => GetField(e);

            [DebuggerStepThrough]
            [NotNull]
            public static MethodInfo Method([NotNull] Expression<Func<Action>> e)
                => GetMethod(e);

            [DebuggerStepThrough]
            [NotNull]
            public static MethodInfo Method<T1>([NotNull] Expression<Func<Action<T1>>> e)
                => GetMethod(e);

            [DebuggerStepThrough]
            [NotNull]
            public static MethodInfo Method<T1, T2>([NotNull] Expression<Func<Action<T1, T2>>> e)
                => GetMethod(e);

            [DebuggerStepThrough]
            [NotNull]
            public static MethodInfo Method<TReturn>([NotNull] Expression<Func<Func<TReturn>>> e)
                => GetMethod(e);

            [DebuggerStepThrough]
            [NotNull]
            public static MethodInfo Method<T1, TReturn>([NotNull] Expression<Func<Func<T1, TReturn>>> e)
                => GetMethod(e);

            [DebuggerStepThrough]
            [NotNull]
            public static MethodInfo Method<T1, T2, TReturn>([NotNull] Expression<Func<Func<T1, T2, TReturn>>> e)
                => GetMethod(e);

            [DebuggerStepThrough]
            [NotNull]
            public static MethodInfo Method<T1, T2, T3, TReturn>([NotNull] Expression<Func<Func<T1, T2, T3, TReturn>>> e)
                => GetMethod(e);
        }

        public static class Instance<T>
        {
            // ReSharper disable once StaticMemberInGenericType
            [NotNull]
            private static readonly Lazy<IReadOnlyCollection<ConstructorInfo>> _constuctors =
                new Lazy<IReadOnlyCollection<ConstructorInfo>>(() => Type.GetConstructors());

            [NotNull]
            public static Type Type { get; } = typeof(T);

            // ReSharper disable once StaticMemberInGenericType
            [NotNull]
            public static string Name { get; } = Type.GetProperName();

            // ReSharper disable once StaticMemberInGenericType
            [NotNull]
            public static string EnumName { get; } = Type.Name.RemoveEnd(Instance<Enum>.Name);

            // ReSharper disable once StaticMemberInGenericType
            [NotNull]
            public static string ControllerName { get; } = Type.Name.RemoveEnd("Controller");

            [DebuggerStepThrough]
            [NotNull]
            public static PropertyInfo Property<TReturn>([NotNull] Expression<Func<T, TReturn>> e)
                => GetProperty(e);

            [DebuggerStepThrough]
            [NotNull]
            public static FieldInfo Field<TReturn>([NotNull] Expression<Func<T, TReturn>> e)
                => GetField(e);

            [DebuggerStepThrough]
            [NotNull]
            public static MethodInfo Method([NotNull] Expression<Func<T, Action>> e)
                => GetMethod(e);

            [DebuggerStepThrough]
            [NotNull]
            public static MethodInfo Method<T1>([NotNull] Expression<Func<T, Action<T1>>> e)
                => GetMethod(e);

            [DebuggerStepThrough]
            [NotNull]
            public static MethodInfo Method<T1, T2>([NotNull] Expression<Func<T, Action<T1, T2>>> e)
                => GetMethod(e);

            [DebuggerStepThrough]
            [NotNull]
            public static MethodInfo Method<TReturn>([NotNull] Expression<Func<T, Func<TReturn>>> e)
                => GetMethod(e);

            [DebuggerStepThrough]
            [NotNull]
            public static MethodInfo Method<T1, TReturn>([NotNull] Expression<Func<T, Func<T1, TReturn>>> e)
                => GetMethod(e);

            [DebuggerStepThrough]
            [NotNull]
            public static MethodInfo Method<T1, T2, TReturn>([NotNull] Expression<Func<T, Func<T1, T2, TReturn>>> e)
                => GetMethod(e);
            
            [DebuggerStepThrough]
            [NotNull]
            public static MethodInfo Indexer<T1, TReturn>([NotNull] Expression<Func<T, T1, TReturn>> e)
                => GetIndexer(e);

            [DebuggerStepThrough]
            [CanBeNull]
            public static ConstructorInfo Constructor<T1>()
                => GetConstructor(_constuctors.Value, Instance<T1>.Type);

            [DebuggerStepThrough]
            [CanBeNull]
            public static ConstructorInfo Constructor<T1, T2>()
                => GetConstructor(_constuctors.Value, Instance<T1>.Type, Instance<T2>.Type);

            [DebuggerStepThrough]
            [CanBeNull]
            public static ConstructorInfo Constructor<T1, T2, T3>()
                => GetConstructor(_constuctors.Value, Instance<T1>.Type, Instance<T2>.Type, Instance<T3>.Type);
        }
    }
}