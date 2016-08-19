using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace piSensorNet.DataModel.Entities.Base
{
    public abstract class EntityBase<TEntity> : EntityBase
        where TEntity : EntityBase<TEntity>
    {
        //public static InsertGenerator<TEntity> GenerateInsert { get; } = GenerateStaticInsertMethod<TEntity>();

        //public static string GenerateUpdate(PiSensorNetDbContext context, IReadOnlyDictionary<Expression<Func<TEntity, object>>, string> values, params Tuple<Expression<Func<TEntity, object>>, string, string>[] predicates)
        //{
        //    var tableName = context.GetTableName<TEntity>();
        //    var columns = values.Select(i => $"`{i.Key.GetMemberName()}` = {i.Value}").Join(", ");
        //    var where = predicates.Select(i => $"`{i.Item1.GetMemberName()}` {i.Item2} {i.Item3}").Join(" AND ");

        //    var update = UpdatePattern.AsFormatFor(tableName, columns, where);

        //    return update;
        //}
    }

    public delegate string InsertGenerator<TEntity>(TEntity entity, string tableName, params KeyValuePair<Expression<Func<TEntity, object>>, string>[] variables)
        where TEntity : EntityBase;

    public abstract class EntityBase
    {
        public const string OnModelCreatingMethodName = "OnModelCreating";

        //protected static readonly MethodInfo StringFormatMethod = Reflector.Instance<String>
        //                                                                   .Type
        //                                                                   .GetMethods(BindingFlags.Public | BindingFlags.Static)
        //                                                                   .Where(i => i.Name.Equals("Format", StringComparison.InvariantCulture))
        //                                                                   .Where(i => i.GetParameters(), i => i.Length == 2
        //                                                                                                       && i[0].ParameterType == Reflector.Instance<string>.Type
        //                                                                                                       && i[1].ParameterType == Reflector.Instance<object[]>.Type)
        //                                                                   .Single();

        //protected static readonly ConstructorInfo StringBuilderConstructor = Reflector.Instance<StringBuilder>
        //                                                                              .Type
        //                                                                              .GetConstructors()
        //                                                                              .Where(i => i.GetParameters(), i => i.Length == 0)
        //                                                                              .Single();

        //protected static readonly MethodInfo MapVariablesMethod = Reflector.Instance<EntityBase>
        //                                                                 .Type
        //                                                                 .GetMethod("MapVariables", BindingFlags.NonPublic | BindingFlags.Static);

        //protected static readonly PropertyInfo IndexerProperty = Reflector.Instance<Dictionary<string, string>>
        //                                                                .Type
        //                                                                .GetProperty("Item");

        //protected static readonly MethodInfo ContainsKeyMethod = Reflector.Instance<Dictionary<string, string>>
        //                                                                .Method<string, bool>(i => i.ContainsKey);


        ///// <summary>
        ///// 0 - table name, 1 - comma-separated list of columns, 2 - comma-separated list of values
        ///// </summary>
        //protected const string InsertPattern = "INSERT INTO `{0}` ({1}) VALUES ({2}); ";

        /// <summary>
        /// 0 - table name, 1 - comma-separated list of assignments, 2 - where clause
        /// </summary>
        protected const string UpdatePattern = "UPDATE `{0}` SET {1} WHERE {2}; ";

        //[UsedImplicitly]
        //private static Dictionary<string, string> MapVariables<TEntity>(KeyValuePair<Expression<Func<TEntity, object>>, string>[] variables)
        //    where TEntity : EntityBase
        //    => variables.ToDictionary(i => i.Key.GetMemberName(), i => i.Value);

        //protected static InsertGenerator<TEntity> GenerateStaticInsertMethod<TEntity>()
        //    where TEntity : EntityBase
        //{
        //    Console.WriteLine($"{DateTime.Now:O}: GenerateInsertMethod<{typeof(TEntity).Name}>()");

        //    var type = Reflector.Instance<TEntity>.Type;
        //    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
        //                         .Where(i => i.CanRead && i.CanWrite)
        //                         .Where(i => !i.GetMethod.IsVirtual && !i.SetMethod.IsVirtual)
        //                         .Where(i => i.GetMethod.IsPublic && i.SetMethod.IsPublic)
        //                         .ToList();

        //    var entityParameter = Expression.Parameter(type, "entity");
        //    var tableNameParameter = Expression.Parameter(Reflector.Instance<String>.Type, "tableName");
        //    var variablesParameter = Expression.Parameter(Reflector.Instance<KeyValuePair<Expression<Func<TEntity, object>>, string>[]>.Type, "variables");
        //    var builderVariable = Expression.Variable(Reflector.Instance<StringBuilder>.Type, "builder");

        //    var patternConstant = Expression.Constant(InsertPattern);
        //    var propertyNamesConstant = Expression.Constant(properties.Select(i => $"`{i.Name}`").Join(", "));

        //    var variablePairsVariable = Expression.Variable(Reflector.Instance<Dictionary<string, string>>.Type, "variablePairs");

        //    var block = new List<Expression>(properties.Count * 2)
        //                {
        //                    Expression.Assign(builderVariable, Expression.New(StringBuilderConstructor)),
        //                    Expression.Assign(variablePairsVariable, Expression.Call(MapVariablesMethod.MakeGenericMethod(Reflector.Instance<TEntity>.Type), variablesParameter)),
        //                };

        //    var lastPropertyIndex = properties.Count - 1;
        //    for (var propertyIndex = 0; propertyIndex < properties.Count; propertyIndex++)
        //    {
        //        var propertyInfo = properties[propertyIndex];
        //        var propertyAccess = Expression.Property(entityParameter, propertyInfo);
        //        var nullableType = propertyInfo.PropertyType.GetNullable();
        //        Expression value;

        //        if (nullableType == null)
        //        {
        //            var toSqlMethod = SqlExtensions.Methods[propertyInfo.PropertyType];
        //            var formattedPropertyExpression = Expression.Call(toSqlMethod, propertyAccess);

        //            value = formattedPropertyExpression;
        //        }
        //        else
        //        {
        //            var toSqlMethod = SqlExtensions.Methods[nullableType];
        //            var ternaryExpression = Expression.Condition(
        //                Expression.Property(propertyAccess, Reflector.Instance<int?>.Property(i => i.HasValue).Name),
        //                Expression.Call(toSqlMethod, Expression.Property(propertyAccess, Reflector.Instance<int?>.Property(i => i.Value).Name)),
        //                Expression.Constant(SqlExtensions.Null));

        //            value = ternaryExpression;
        //        }

        //        value = Expression.Condition(
        //            Expression.Call(variablePairsVariable, ContainsKeyMethod, Expression.Constant(propertyInfo.Name)),
        //            Expression.MakeIndex(variablePairsVariable, IndexerProperty, new[] { Expression.Constant(propertyInfo.Name) }),
        //            value);

        //        block.Add(Expression.Call(builderVariable, Reflector.Instance<StringBuilder>.Method<string, StringBuilder>(i => i.Append), value));

        //        if (propertyIndex >= lastPropertyIndex)
        //            continue;

        //        block.Add(Expression.Call(builderVariable, Reflector.Instance<StringBuilder>.Method<string, StringBuilder>(i => i.Append), Expression.Constant(", ")));
        //    }

        //    var propertyValuesCall = Expression.Call(builderVariable, Reflector.Instance<StringBuilder>.Method<string>(i => i.ToString));
        //    var argumentsArray = Expression.NewArrayInit(Reflector.Instance<object>.Type, tableNameParameter, propertyNamesConstant, propertyValuesCall);

        //    block.Add(Expression.Call(StringFormatMethod, patternConstant, argumentsArray));

        //    var lambda = Expression.Lambda<InsertGenerator<TEntity>>(
        //        Expression.Block(new[] { builderVariable, variablePairsVariable }, block),
        //        entityParameter, tableNameParameter, variablesParameter);

        //    return lambda.Compile();
        //}
    }
}
