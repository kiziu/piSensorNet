using System;
using System.Linq;
using piSensorNet.Web.Models.Base;

namespace piSensorNet.Web.Models
{
    public sealed class TriggerListItemModel : BaseModel
    {
        public int ID { get; set; }

        public string FriendlyName { get; set; }

        public string Description { get; set; }

        public DateTime LastModified { get; set; }

        public DateTime Created { get; set; }
    }
}