//#define CATCH_VALIDATION_ERRORS

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Runtime.CompilerServices;
using piSensorNet.Common.Extensions;
using piSensorNet.Common.System;
using piSensorNet.DataModel.Entities.Base;

[assembly: InternalsVisibleTo("piSensorNet.Tests")]

namespace piSensorNet.DataModel.Context
{
    [DbConfigurationType(typeof(PiSensorNetDbConfiguration))]
    public partial class PiSensorNetDbContext : DbContext
    {
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

        private PiSensorNetDbContext(string nameOrConnectionString, IDatabaseInitializer<PiSensorNetDbContext> initializer)
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

        public PiSensorNetDbContext(string nameOrConnectionString)
            : this(nameOrConnectionString, null) {}

        public static PiSensorNetDbContext Connect(string nameOrConnectionString) => new PiSensorNetDbContext(nameOrConnectionString, null);

        public static void Initialize(string nameOrConnectionString, bool recreateDatabase = false)
        {
            using (var context = new PiSensorNetDbContext(nameOrConnectionString, recreateDatabase ? RecreateInitializer : null))
            {
                // ReSharper disable once UnusedVariable
                var dummy = context.Packets.FirstOrDefault();
            }
        }

        public PiSensorNetDbContext WithChangeTracking()
        {
            ChangeTracking = true;

            return this;
        }

        public PiSensorNetDbContext WithLazyLoading()
        {
            Configuration.LazyLoadingEnabled = true;

            return this;
        }

        public string GetTableName<TEntity>()
            where TEntity : EntityBase => GetTableName(Reflector.Instance<TEntity>.Type);

        public string GetTableName(Type entityType)
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

        public void EnqueueRaw(string sql) => _rawQueries.Add(sql);

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
    }
}