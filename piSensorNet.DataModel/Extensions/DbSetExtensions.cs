using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using piSensorNet.DataModel.Entities.Base;

namespace piSensorNet.DataModel.Extensions
{
    public static class DbSetExtensions
    {
        public static IEnumerable<TEntity> Add<TEntity>(this DbSet<TEntity> dbSet, params TEntity[] entities)
            where TEntity : EntityBase
            => dbSet.AddRange(entities);
    }
}
