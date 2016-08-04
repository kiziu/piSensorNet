using System;
using System.Data.Entity.Migrations;
using System.Linq;

namespace piSensorNet.DataModel.Context
{
    internal sealed class PiSensorNetDbMigrationConfiguration : DbMigrationsConfiguration<PiSensorNetDbContext>
    {
        public PiSensorNetDbMigrationConfiguration()
        {
            AutomaticMigrationsEnabled = false;
        }
    }
}