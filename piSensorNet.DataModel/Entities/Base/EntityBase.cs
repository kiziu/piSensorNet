using System;
using System.Linq;

namespace piSensorNet.DataModel.Entities.Base
{
    public abstract class EntityBase
    {
        public const string OnModelCreatingMethodName = "OnModelCreating";

        /// <summary>
        /// 0 - table name, 1 - comma-separated list of assignments, 2 - where clause
        /// </summary>
        public const string UpdatePattern = "UPDATE `{0}` SET {1} WHERE {2}; ";
    }
}
