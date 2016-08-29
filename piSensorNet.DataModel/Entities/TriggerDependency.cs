using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using piSensorNet.Common.Enums;
using piSensorNet.DataModel.Entities.Base;

namespace piSensorNet.DataModel.Entities
{
    public class TriggerDependency : EntityBase
    {
        internal static void OnModelCreating(EntityTypeConfiguration<TriggerDependency> entityTypeConfiguration)
        {
            entityTypeConfiguration.HasRequired(i => i.Trigger)
                                   .WithMany(i => i.TriggerDependencies)
                                   .HasForeignKey(i => i.TriggerID);
        }

        protected TriggerDependency() {}

        public TriggerDependency(int triggerID, TriggerDependencyTypeEnum type)
        {
            TriggerID = triggerID;
            Type = type;
        }

        [Key]
        [Column(Order = 0)]
        public int TriggerID { get; set; }

        [Key]
        [Column(Order = 1)]
        public TriggerDependencyTypeEnum Type { get; set; }

        public virtual Trigger Trigger { get; set; }
    }
}