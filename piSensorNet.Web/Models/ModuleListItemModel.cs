using System;
using piSensorNet.Common.Enums;
using piSensorNet.Web.Models.Base;

namespace piSensorNet.Web.Models
{
    public sealed class ModuleListItemModel : BaseModel
    {
        public int ID { get; set; }
        
        public string FriendlyName { get; set; }
        
        public string Description { get; set; } 
        
        public string Address { get; set; }

        public ModuleStateEnum State { get; set; }

        public DateTime Created { get; set; }
    }
}