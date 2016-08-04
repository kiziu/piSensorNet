using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using piSensorNet.Common.System;
using piSensorNet.DataModel.Entities.Base;

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

        private readonly List<string> _queries = new List<string>();

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
            where TEntity : EntityBase
        {
            var typeOf = typeof(TEntity);

            return GetTableName(typeOf);
        }

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
        
        public void EnqueueQuery(string sql) => _queries.Add(sql);

        public void ExecuteQueries()
        {
            _queries.ForEach(i => Database.ExecuteSqlCommand(i));
            _queries.Clear();
        }

        public override int SaveChanges()
        {
            var result =  base.SaveChanges();

            ExecuteQueries();

            return result;

            //try
            //{
            //    return base.SaveChanges();
            //}
            //catch (DbEntityValidationException e)
            //{
            //    var errors = e.EntityValidationErrors
            //                  .SelectMany(i => i.ValidationErrors)
            //                  .Select(i => $"{i.PropertyName}: {i.ErrorMessage}")
            //                  .Join(Environment.NewLine);

            //    throw new DbEntityValidationException($"{e.Message}{Environment.NewLine}{errors}", e.EntityValidationErrors, e.InnerException);
            //}
        }
    }
}