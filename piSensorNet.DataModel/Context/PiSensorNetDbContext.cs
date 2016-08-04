using System;
using System.Collections.Concurrent;
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
        private static readonly PiSensorNetDbContextInitializer RecreateInitializer = new PiSensorNetDbContextInitializer();

        public static string Identity => "LAST_INSERT_ID()";
        public static string SelectIdentity => "SELECT " + Identity;
        public static string SetIdentityVariable(string name) => $"SET {name} = ({Identity})";

        public static Action<string> Logger { get; set; } = null;

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
            if (Logger != null)
                Database.Log = Logger;
            else if (Constants.IsWindows && initializer != null)
                Database.Log = Console.Write;
            else if (Constants.IsWindows)
                Database.Log = i => System.Diagnostics.Debug.Write(i);

            Database.SetInitializer(initializer);

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

        private static readonly ConcurrentDictionary<Type, string> TableNames = new ConcurrentDictionary<Type, string>();
        public string GetTableName(Type entityType)
        {
            return TableNames.GetOrAdd(entityType, type =>
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

        public static string GetMemberName<TEntity>(Expression<Func<TEntity, object>> expression)
            where TEntity : EntityBase
        {
            MemberExpression body = null;

            var unaryExpression = expression.Body as UnaryExpression;
            if (unaryExpression != null)
            {
                if (unaryExpression.NodeType == ExpressionType.Convert || unaryExpression.NodeType == ExpressionType.ConvertChecked)
                    body = (MemberExpression)unaryExpression.Operand;
            }
            else if (expression.Body is MemberExpression)
                body = (MemberExpression)expression.Body;

            if (body == null)
                throw new InvalidOperationException(
                    $"Expression '{expression}' is not valid version of {typeof(MemberExpression).Name}.");

            var member = body.Member;

            return member.Name;
        }

        // TODO KZ: remove when done
        //public override int SaveChanges()
        //{
        //    try
        //    {
        //        return base.SaveChanges();
        //    }
        //    catch (DbEntityValidationException e)
        //    {
        //        var errors = e.EntityValidationErrors
        //                      .SelectMany(i => i.ValidationErrors)
        //                      .Select(i => $"{i.PropertyName}: {i.ErrorMessage}")
        //                      .Join(Environment.NewLine);

        //        throw new DbEntityValidationException($"{e.Message}{Environment.NewLine}{errors}", e.EntityValidationErrors, e.InnerException);
        //    }
        //}
    }
}