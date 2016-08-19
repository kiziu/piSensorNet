using System;
using System.Linq;
using piSensorNet.DataModel.Context;

namespace piSensorNet.Logic.TriggerSourceHandlers.Base
{
    public class TriggerSourceHandlerContext
    {
        public TriggerSourceHandlerContext(PiSensorNetDbContext databaseContext)
        {
            DatabaseContext = databaseContext;
        }

        public PiSensorNetDbContext DatabaseContext { get; }
    }
}