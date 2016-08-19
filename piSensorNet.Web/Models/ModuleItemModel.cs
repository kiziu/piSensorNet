using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using piSensorNet.Common.Enums;

namespace piSensorNet.Web.Models
{
    public class ModuleItemModel
    {
        [ReadOnly(true)]
        public int ID { get; set; }
        
        [StringLength(50)]
        public string FriendlyName { get; set; }

        [StringLength(500)]
        public string Description { get; set; } 
        
        [ReadOnly(true)]
        public string Address { get; set; }

        [ReadOnly(true)]
        public ModuleStateEnum State { get; set; }

        [ReadOnly(true)]
        public DateTime Created { get; set; }
    }
}