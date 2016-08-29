using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using piSensorNet.Common.Custom;
using piSensorNet.Common.Enums;
using piSensorNet.Common.Extensions;
using piSensorNet.Common.System;
using piSensorNet.DataModel.Context;

namespace piSensorNet.Logic.TriggerDependencyHandlers.Base
{
    internal abstract class BaseTriggerDependencyHandler<THandler> : ITriggerDependencyHandler
        where THandler : BaseTriggerDependencyHandler<THandler>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly IReadOnlyDictionary<string, Type> properties;

        private static readonly Func<BaseTriggerDependencyHandler<THandler>, IReadOnlyDictionary<string, TypedObject>> toProperties;

        static BaseTriggerDependencyHandler()
        {
            var fields = typeof(THandler).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            properties = fields.ToDictionary(i => i.Name, i => i.FieldType);

            {
                var parameter = Expression.Parameter(Reflector.Instance<BaseTriggerDependencyHandler<THandler>>.Type, "instance");
                var cast = Expression.Convert(parameter, Reflector.Instance<THandler>.Type);
                var dictionary = Expression.Variable(Reflector.Instance<Dictionary<string, TypedObject>>.Type, "result");
                var assign = Expression.Assign(
                    dictionary,
                    Expression.New(Reflector.Instance<Dictionary<string, TypedObject>>.Constructor<int>(),
                        Expression.Constant(fields.Length)));

                var addMethod = Reflector.Instance<Dictionary<string, TypedObject>>.Method<string, TypedObject>(i => i.Add);
                var typedObjectCreator = Reflector.Static.Method<object, Type, TypedObject>(() => TypedObject.Create);
                var block = Expression.Block(new[] {dictionary},
                    new List<Expression>
                    {
                        assign,
                        fields.Select(i =>
                            Expression.Call(dictionary, addMethod,
                                Expression.Constant(i.Name),
                                Expression.Call(typedObjectCreator,
                                    Expression.Field(cast, i),
                                    Expression.Constant(i.FieldType))))
                              .Cast<Expression>(),
                        dictionary,
                    });

                toProperties = Expression.Lambda<Func<BaseTriggerDependencyHandler<THandler>, IReadOnlyDictionary<string, TypedObject>>>(block, parameter)
                                         .Compile();
            }
        }

        public IReadOnlyDictionary<string, Type> Properties { get; } = properties;

        public abstract TriggerDependencyTypeEnum TriggerDependencyType { get; }
        public abstract bool IsModuleIdentityRequired { get; }
        public abstract IReadOnlyDictionary<string, TypedObject> Handle(PiSensorNetDbContext context, int? moduleID);

        //protected IReadOnlyDictionary<string, TypedObject> ToProperties() 
        //    => fields.ToDictionary(i => i.Name, i => i.GetValue(this).ToTyped(i.FieldType));

        protected IReadOnlyDictionary<string, TypedObject> ToProperties()
            => toProperties(this);
    }
}