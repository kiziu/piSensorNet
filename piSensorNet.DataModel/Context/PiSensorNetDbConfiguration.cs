using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Migrations.History;
using System.Data.Entity.Migrations.Model;
using System.Data.Entity.Migrations.Sql;
using System.Linq;
using MySql.Data.Entity;
using MySql.Data.MySqlClient;
using piSensorNet.Common.Extensions;

namespace piSensorNet.DataModel.Context
{
    internal sealed class PiSensorNetDbConfiguration : DbConfiguration
    {
        public PiSensorNetDbConfiguration()
        {
            AddDependencyResolver(new MySqlDependencyResolver());
            SetProviderFactory(MySqlProviderInvariantName.ProviderName, new MySqlClientFactory());
            SetProviderServices(MySqlProviderInvariantName.ProviderName, new MySqlProviderServices());
            SetDefaultConnectionFactory(new MySqlConnectionFactory());
            SetMigrationSqlGenerator(MySqlProviderInvariantName.ProviderName, () => new DehistorifiedMySqlMigrationSqlGenerator());
            SetProviderFactoryResolver(new MySqlProviderFactoryResolver());
            SetManifestTokenResolver(new MySqlManifestTokenResolver());
            SetHistoryContext(MySqlProviderInvariantName.ProviderName, (existingConnection, defaultSchema) => new MySqlHistoryContextProxy(existingConnection, defaultSchema));
        }

        private sealed class MySqlHistoryContextProxy : MySqlHistoryContext
        {
            public MySqlHistoryContextProxy(DbConnection existingConnection, string defaultSchema)
                : base(existingConnection, defaultSchema) {}
        }

        private sealed class DehistorifiedMySqlMigrationSqlGenerator : MySqlMigrationSqlGenerator
        {
            protected override MigrationStatement Generate(CreateTableOperation op)
            {
                if (op.Name.Contains(HistoryContext.DefaultTableName, StringComparison.InvariantCultureIgnoreCase))
                    return new MigrationStatement();

                return base.Generate(op);
            }

            protected override MigrationStatement Generate(HistoryOperation op)
            {
                for (var i = op.CommandTrees.Count - 1; i >= 0; --i)
                {
                    var dbScanExpression = (DbScanExpression)op.CommandTrees[i].Target.Expression;

                    if (dbScanExpression.Target
                                        .Table
                                        .Equals(HistoryContext.DefaultTableName, StringComparison.InvariantCultureIgnoreCase))
                        op.CommandTrees.RemoveAt(i);
                }

                return base.Generate(op);
            }
        }
    }
}