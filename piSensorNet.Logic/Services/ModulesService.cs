using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.DataModel.Context;

namespace piSensorNet.Logic.Services
{
    public class ModulesService
    {
        private readonly Func<PiSensorNetDbContext> _contextFactory;

        public ModulesService(Func<PiSensorNetDbContext> contextFactory)
        {
            if (contextFactory == null) throw new ArgumentNullException(nameof(contextFactory));

            _contextFactory = contextFactory;
        }

        public IReadOnlyDictionary<int, string> List()
        {
            using (var context = _contextFactory())
            {
                var sensors = context.Modules
                                     .OrderBy(i => i.FriendlyName)
                                     .ThenBy(i => i.Address)
                                     .ToDictionary(i => i.ID, i => i.FriendlyName ?? i.Address);

                return sensors;
            }
        }
    }
}
