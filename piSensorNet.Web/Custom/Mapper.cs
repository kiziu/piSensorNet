using System;
using System.Linq;
using JetBrains.Annotations;
using piSensorNet.DataModel.Entities;
using piSensorNet.Web.Models;

namespace piSensorNet.Web.Custom
{
    public static class Mapper
    {
        [NotNull]
        public static ModuleListItemModel ToListItem([NotNull] this Module o)
        {
            return new ModuleListItemModel
                   {
                       ID = o.ID,
                       Address = o.Address,
                       Description = o.Description,
                       FriendlyName = o.FriendlyName,
                       State = o.State,
                       Created = o.Created,
                   };
        }

        [NotNull]
        public static TriggerListItemModel ToListItem([NotNull] this Trigger o)
        {
            return new TriggerListItemModel
                   {
                       ID = o.ID,
                       FriendlyName = o.FriendlyName,
                       Description = o.Description,
                       Created = o.Created
                   };
        }

        [NotNull]
        public static ModuleItemModel MapToItem([NotNull] this Module o)
        {
            return new ModuleItemModel
                   {
                       ID = o.ID,
                       Address = o.Address,
                       Description = o.Description,
                       FriendlyName = o.FriendlyName,
                       State = o.State,
                       Created = o.Created,
                   };
        }
    }
}