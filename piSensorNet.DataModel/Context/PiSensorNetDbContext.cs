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
        static PiSensorNetDbContext()
        {
            Database.SetInitializer(new NullDatabaseInitializer<PiSensorNetDbContext>());
        }

        private static readonly PiSensorNetDbContextInitializer RecreateInitializer = new PiSensorNetDbContextInitializer();
        private static readonly ConcurrentDictionary<Type, string> TableNamesCache = new ConcurrentDictionary<Type, string>();

        public static string Identity => "LAST_INSERT_ID()";
        public static string SelectIdentity => "SELECT " + Identity;
        public static string SetIdentityVariable(string name) => $"SET {name} = ({Identity})";

        public static Action<string> Logger { get; set; } = null;

        public bool AutoSaveEnabled { get; private set; }

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
        public PiSensorNetDbContext WithAutoSave()
        {
            AutoSaveEnabled = true;

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

            var where = ExpressionExtensions.ExtractWhereClauseFromPredicate(predicate.Body, new StringBuilder(), SqlExtensions.Formatters);

            var update = EntityBase.UpdatePattern.AsFormatFor(tableName, columns, where);

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

        protected override void Dispose(bool disposing)
        {
            if (AutoSaveEnabled)
                SaveChanges();

            base.Dispose(disposing);
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