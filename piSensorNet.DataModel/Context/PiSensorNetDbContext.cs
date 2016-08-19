//#define CATCH_VALIDATION_ERRORS

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using piSensorNet.Common.Extensions;
using piSensorNet.Common.System;
using piSensorNet.DataModel.Entities.Base;
using piSensorNet.DataModel.Extensions;

[assembly: InternalsVisibleTo("piSensorNet.Tests")]

namespace piSensorNet.DataModel.Context
{
    [DbConfigurationType(typeof(PiSensorNetDbConfiguration))]
    public partial class PiSensorNetDbContext : DbContext
    {
        protected const string UpdatePattern = "UPDATE `{0}` SET {1} WHERE {2}; ";

        //private static readonly IReadOnlyDictionary<Type, Func<IQueryable, Tuple<string, ObjectParameterCollection>>> QueryExtractors;

        static PiSensorNetDbContext()
        {
            Database.SetInitializer(new NullDatabaseInitializer<PiSensorNetDbContext>());

            //InitQueryExtractors();
        }

        //private static void InitQueryExtractors()
        //{
        //    var internalQueryType = Type.GetType("System.Data.Entity.Internal.Linq.InternalQuery`1, EntityFramework");
        //    var objectQueryStateType = Type.GetType("System.Data.Entity.Core.Objects.Internal.ObjectQueryState, EntityFramework") ?? typeof(void);
        //    QueryExtractors = EntityTypes.ToDictionary(i => i, i =>
        //    {
        //        var parameter = Expression.Parameter(typeof(IQueryable), "query");
        //        var convertedParameter = Expression.Convert(parameter, typeof(DbQuery<>).MakeGenericType(i));
        //        var internalQueryField = Expression.Field(convertedParameter, "_internalQuery");
        //        var convertedInternalQueryField = Expression.Convert(internalQueryField, internalQueryType.MakeGenericType(i));
        //        var objectQueryfield = Expression.Field(convertedInternalQueryField, "_objectQuery");
        //        var convertedObjectQueryfield = Expression.Convert(objectQueryfield, typeof(ObjectQuery));
        //        var stateField = Expression.Field(convertedObjectQueryfield, "_state");
        //        var stateFieldVariable = Expression.Variable(objectQueryStateType, "state");
        //        var executionPlan = Expression.Call(stateFieldVariable, "GetExecutionPlan", null, Expression.Constant(null, typeof(MergeOption?)));
        //        var query = Expression.Call(executionPlan, "ToTraceString", null);
        //        //var query = Expression.Constant("SELECT * FROM `Messages` AS `Extent1` WHERE `Extent1`.`ID` = 19");
        //        var parameters = Expression.Field(stateFieldVariable, "_parameters");
        //        var tuple = Expression.New(typeof(Tuple<string, ObjectParameterCollection>).GetConstructors().Single(), query, parameters);

        //        var block = Expression.Block(new[] { stateFieldVariable },
        //            Expression.Assign(stateFieldVariable, stateField),
        //            tuple);

        //        var method = Expression.Lambda<Func<IQueryable, Tuple<string, ObjectParameterCollection>>>(block, parameter)
        //                               .Compile();

        //        return method;
        //    });
        //}

        private static readonly PiSensorNetDbContextInitializer RecreateInitializer = new PiSensorNetDbContextInitializer();
        private static readonly ConcurrentDictionary<Type, string> TableNamesCache = new ConcurrentDictionary<Type, string>();

        public static string Identity => "LAST_INSERT_ID()";
        public static string SelectIdentity => "SELECT " + Identity;
        public static string SetIdentityVariable(string name) => $"SET {name} = ({Identity})";

        public static Action<string> Logger { get; set; } = null;

        private readonly List<string> _rawQueries = new List<string>();

        public bool ChangeTracking
        {
            get { return Configuration.AutoDetectChangesEnabled && Configuration.ProxyCreationEnabled; }
            set
            {
                Configuration.AutoDetectChangesEnabled = value;
                Configuration.ProxyCreationEnabled = value;
            }
        }

        private PiSensorNetDbContext([NotNull] string nameOrConnectionString, [CanBeNull] IDatabaseInitializer<PiSensorNetDbContext> initializer)
            : base(nameOrConnectionString)
        {
            if (initializer != null)
                Database.SetInitializer(initializer);

            if (Logger != null)
                Database.Log = Logger;
            else if (Constants.IsWindows && initializer != null)
                Database.Log = Console.Write;
            else if (Constants.IsWindows)
                Database.Log = i => System.Diagnostics.Debug.Write(i);

            ChangeTracking = false;

            Configuration.UseDatabaseNullSemantics = false;
            Configuration.LazyLoadingEnabled = false;
            Configuration.ValidateOnSaveEnabled = false;
        }

        public PiSensorNetDbContext([NotNull] string nameOrConnectionString)
            : this(nameOrConnectionString, null) {}

        [NotNull]
        public static PiSensorNetDbContext Connect([NotNull] string nameOrConnectionString) => new PiSensorNetDbContext(nameOrConnectionString, null);

        public static void Initialize([NotNull] string nameOrConnectionString, bool recreateDatabase = false)
        {
            using (var context = new PiSensorNetDbContext(nameOrConnectionString, recreateDatabase ? RecreateInitializer : null))
            {
                // ReSharper disable once UnusedVariable
                var dummy = context.Packets.FirstOrDefault();
            }
        }

        [NotNull]
        public PiSensorNetDbContext WithChangeTracking()
        {
            ChangeTracking = true;

            return this;
        }

        [NotNull]
        public PiSensorNetDbContext WithLazyLoading()
        {
            Configuration.LazyLoadingEnabled = true;

            return this;
        }

        [NotNull]
        public string GetTableName<TEntity>()
            where TEntity : EntityBase => GetTableName(Reflector.Instance<TEntity>.Type);

        [NotNull]
        public string GetTableName([NotNull] Type entityType)
        {
            return TableNamesCache.GetOrAdd(entityType, type =>
            {
                var objectContext = ((IObjectContextAdapter)this).ObjectContext;
                var container = objectContext.MetadataWorkspace.GetEntityContainer(objectContext.DefaultContainerName, DataSpace.CSpace);
                var entitySetBase = container.BaseEntitySets
                                             .Where(ii => ii.BuiltInTypeKind == BuiltInTypeKind.EntitySet)
                                             .Where(ii => ii.ElementType.Name == type.Name)
                                             .Single();

                return entitySetBase.Name;
            });
        }

        [NotNull]
        public string EnqueueUpdate<TEntity>([InstantHandle] [NotNull] Expression<Func<TEntity, bool>> values, [InstantHandle] [NotNull] Expression<Func<TEntity, bool>> predicate)
            where TEntity : EntityBase
        {
            var tableName = GetTableName<TEntity>();

            var valuesProperties = ExpressionExtensions.ExtractPropertiesFromEqualityComparisons(values.Body);
            var columns = valuesProperties.Select(i => $"`{i.Key.Name}` = {SqlExtensions.Formatters[i.Key.PropertyType](i.Value)}").Join(", ");

            var where = ExtractPredicate(predicate.Body, new StringBuilder());

            var update = UpdatePattern.AsFormatFor(tableName, columns, where);

            return update;
        }

        public void EnqueueRaw([NotNull] string sql) => _rawQueries.Add(sql);

        public void ExecuteRaw()
        {
            if (_rawQueries.Count == 0)
                return;

            Database.ExecuteSqlCommand(_rawQueries.Concat());
            _rawQueries.Clear();
        }

        //public string Update<TEntity>(Func<PiSensorNetDbContext, IQueryable<TEntity>> entitiesSelector, Expression<Func<TEntity, bool>> updates)
        //    where TEntity : EntityBase
        //{
        //    var query = entitiesSelector(this);
        //    var queryTuple = QueryExtractors[Reflector.Instance<TEntity>.Type](query);

        //    var builder = new StringBuilder(queryTuple.Item1);

        //    if (queryTuple.Item2 != null)
        //        foreach (var p in queryTuple.Item2)
        //            builder.Replace("@" + p.Name, p.Value.ToSql(p.ParameterType));

        //    var fromWhere = builder.ToString().SubstringAfter("FROM ", true) + ";";
        //    var wherePosition = fromWhere.IndexOf("WHERE ", StringComparison.InvariantCulture);
        //    var from = fromWhere.Substring(0, wherePosition).Trim();
        //    var where = fromWhere.Substring(wherePosition).Trim();
        //    var alias = from.SubstringAfter("AS `", true).Substring(3);

        //    var properties = Traverse(updates.Body, new Dictionary<PropertyInfo, object>());

        //    var update = "UPDATE " + from.Substring("FROM ".Length);
        //    var set = "SET " + properties.Select(i => $"{alias}.`{i.Key.Name}` = {i.Value.ToSql(i.Key.PropertyType)}").Join(", ");

        //    var result = String.Join(Environment.NewLine, update, set, where);

        //    EnqueueRaw(result);

        //    return result;
        //}

        public override int SaveChanges()
        {
#if CATCH_VALIDATION_ERRORS
            int result;
            try
            {
                result = base.SaveChanges();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException e)
            {
                var errors = e.EntityValidationErrors
                              .SelectMany(i => i.ValidationErrors)
                              .Select(i => $"{i.PropertyName}: {i.ErrorMessage}")
                              .Join(Environment.NewLine);

                throw new System.Data.Entity.Validation.DbEntityValidationException($"{e.Message}{Environment.NewLine}{errors}", e.EntityValidationErrors, e.InnerException);
            }
#else
            var result = base.SaveChanges();
#endif

            ExecuteRaw();

            return result;

        }
        
        private static StringBuilder ExtractPredicate(Expression e, StringBuilder builder)
        {
            var binary = e as BinaryExpression;
            if (binary == null)
            {
                var methodCall = e as MethodCallExpression;
                if (methodCall == null)
                    return builder;

                if (methodCall.Method.Name != "Contains")
                    throw new NotSupportedException($"Method '{methodCall.Method.Name}' is not supported;");

                if (methodCall.Arguments.Count != 2 || methodCall.Arguments[1].Type != Reflector.Instance<int>.Type)
                    throw new NotSupportedException($"Arguments [{methodCall.Arguments.Select(i => $"{i.Type}").Join(", ")}] are not supported.");

                var propertyArgument = (MemberExpression)methodCall.Arguments[1];
                var formatter = SqlExtensions.Formatters[propertyArgument.Member.GetMemberType()];
                var valuesSelector = Expression.Lambda<Func<IEnumerable>>(methodCall.Arguments[0]).Compile();
                var values = valuesSelector().Cast<object>().Select(formatter).Join(", ");

                builder.Append($"`{propertyArgument.Member.Name}` IN ({values})");

                return builder;
            }

            var property = binary.Left as MemberExpression;
            if (property == null)
            {
                var unary = binary.Left as UnaryExpression;
                if (unary != null && unary.NodeType == ExpressionType.Convert)
                    property = unary.Operand as MemberExpression;
            }

            if (property != null)
            {
                var value = binary.Right;

                var unary = value as UnaryExpression;
                if (unary != null && unary.NodeType == ExpressionType.Convert)
                    value = unary.Operand;

                var propertyName = property.Member.Name;
                var nestedProperty = property.Expression as MemberExpression;
                if (nestedProperty != null)
                    throw new NotSupportedException($"Nested '{Reflector.Instance<MemberExpression>.Name}' in '{e}' not supported.");

                string op;
                switch (e.NodeType)
                {
                    case ExpressionType.Equal:
                        op = "=";
                        break;

                    case ExpressionType.NotEqual:
                        op = "!=";
                        break;

                    case ExpressionType.GreaterThan:
                        op = ">";
                        break;

                    case ExpressionType.GreaterThanOrEqual:
                        op = ">=";
                        break;

                    case ExpressionType.LessThan:
                        op = "<";
                        break;

                    case ExpressionType.LessThanOrEqual:
                        op = "<=";
                        break;

                    default:
                        throw new NotSupportedException($"Operand '{e.NodeType}' is not supported yet.");
                }

                var constantValue = value as ConstantExpression;
                if (constantValue != null)
                {
                    builder.Append($"`{propertyName}` {op} {SqlExtensions.Formatters[constantValue.Type](constantValue.Value)}");
                    return builder;
                }

                var member = value as MemberExpression;
                if (member != null)
                {
                    var valueGetter = Expression.Lambda<Func<object>>(Expression.Convert(member, typeof(object))).Compile();

                    builder.Append($"`{propertyName}` {op} {SqlExtensions.Formatters[member.Member.GetMemberType()](valueGetter())}");
                    // ReSharper disable once RedundantJumpStatement
                    return builder;
                }
            }
            else if (e.NodeType == ExpressionType.AndAlso || e.NodeType == ExpressionType.OrElse)
            {
                builder.Append("(");

                ExtractPredicate(binary.Left, builder);

                builder.Append(e.NodeType == ExpressionType.AndAlso ? " AND " : " OR ");

                // ReSharper disable once TailRecursiveCall
                ExtractPredicate(binary.Right, builder);

                builder.Append(")");
            }

            return builder;
        }

        public static void CheckCompatibility(string nameOrConnectionString)
        {
            using (var context = new PiSensorNetDbContext(nameOrConnectionString, null))
            {
                var tables = Reflector.Instance<PiSensorNetDbContext>.Type
                                      .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                      .Where(i => i.PropertyType.IsGenericType
                                                  && i.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                                      .ToList();

                var exceptions = new List<string>(tables.Count);
                var firstOrDefault = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                      .Where(i => i.Name.Equals("FirstOrDefault", StringComparison.InvariantCulture)
                                                                  && i.GetParameters().Length == 1)
                                                      .Single();

                foreach (var table in tables)
                {
                    var entityType = table.PropertyType.GetGenericArguments()[0];
                    var value = table.GetValue(context);

                    try
                    {
                        firstOrDefault.MakeGenericMethod(entityType).Invoke(null, new[] {value});
                    }
                    catch (TargetInvocationException e)
                    {
                        var entityCommandExecutionException = e.InnerException as EntityCommandExecutionException;
                        if (entityCommandExecutionException != null)
                            exceptions.Add(table.Name + ": " + entityCommandExecutionException.InnerException.Message);
                        else
                            exceptions.Add(table.Name + ": " + e.InnerException.Message);
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(table.Name + ": " + e.Message);
                    }
                }

                if (exceptions.Count > 0)
                    throw new NotSupportedException($"Errors found:{Environment.NewLine}\t- {exceptions.Join(Environment.NewLine + "\t- ")}");
            }
        }
    }
}