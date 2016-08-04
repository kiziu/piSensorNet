using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Reflection;
using piSensorNet.Common.System;
using piSensorNet.DataModel.Entities.Base;

namespace piSensorNet.DataModel.Context
{
    public partial class PiSensorNetDbContext
    {
        private static readonly Type TypeOfVoid = typeof(void);
        private static readonly Type TypeOfEntityTypeConfiguration = typeof(EntityTypeConfiguration<>);

        private static readonly MethodInfo EntityMethod = Reflector.Instance<DbModelBuilder>
                                                                   .Method<EntityTypeConfiguration<EntityBase>>(
                                                                       i => i.Entity<EntityBase>)
                                                                   .GetGenericMethodDefinition();

        internal static readonly Type EntityBaseType = Reflector.Instance<EntityBase>.Type;
        internal static readonly IReadOnlyCollection<Type> EntityTypes = EntityBaseType.Assembly
                                                                                       .GetTypes()
                                                                                       .Where(EntityBaseType.IsAssignableFrom)
                                                                                       .Where(i => !i.IsAbstract && i.IsClass)
                                                                                       .ToList();

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Properties<DateTime>().Configure(i => i.HasPrecision(3));
            modelBuilder.Properties<DateTime?>().Configure(i => i.HasPrecision(3));
            
            foreach (var entityType in EntityTypes)
            {
                var method = entityType.GetMethod(EntityBase.OnModelCreatingMethodName, BindingFlags.NonPublic | BindingFlags.Static);
                if (method == null)
                    continue;

                var hasProperSignature = HasProperSignature(entityType, method);
                if (hasProperSignature > 0)
                    throw new InvalidOperationException(
                        $"Method '{entityType.Name}.{method.Name}' has incorrect signature. Error {hasProperSignature}.");

                var entityMethod = EntityMethod.MakeGenericMethod(entityType);
                var entityTypeConfiguration = entityMethod.Invoke(modelBuilder, null);

                method.Invoke(null, new[] {entityTypeConfiguration});
            }
        }

        private static int HasProperSignature(Type entityType, MethodInfo method)
        {
            if (method.ReturnType != TypeOfVoid)
                return 1;

            var methodParameters = method.GetParameters();
            if (methodParameters.Length != 1 )
                return 2;
            
            var parameterType = methodParameters[0].ParameterType;
            if (!parameterType.IsConstructedGenericType)
                return 3;

            var genericTypeArguments = parameterType.GetGenericArguments();
            if (genericTypeArguments.Length != 1)
                return 4;

            var genericType = genericTypeArguments[0];
            var parameterOpenType = parameterType.GetGenericTypeDefinition();

            if (parameterOpenType != TypeOfEntityTypeConfiguration)
                return 5;

            if (genericType != entityType)
                return 6;

            return 0;
        }
    }
}