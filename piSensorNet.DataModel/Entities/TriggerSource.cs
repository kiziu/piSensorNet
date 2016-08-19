using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using piSensorNet.Common.Enums;
using piSensorNet.DataModel.Entities.Base;

namespace piSensorNet.DataModel.Entities
{
    public class TriggerSource : EntityBase
    {
        internal static void OnModelCreating(EntityTypeConfiguration<TriggerSource> entityTypeConfiguration)
        {
            entityTypeConfiguration.Property(i => i.AbsoluteTime).HasPrecision(0);

            entityTypeConfiguration.HasRequired(i => i.Trigger)
                                   .WithMany(i => i.TriggerSources)
                                   .HasForeignKey(i => i.TriggerID);
        }

        protected TriggerSource() {}

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; protected internal set; }

        public int TriggerID { get; set; }
        
        public TriggerSourceTypeEnum Type { get; set; } = TriggerSourceTypeEnum.Unknown;

        public DateTime Created { get; set; } = DateTime.Now;

        public DateTime? NextAbsoluteTimeExecution { get; set; } = null;
        public TimeSpan? AbsoluteTime { get; set; } = null;

        public int? TemperatureSensorID { get; set; } = null;


        public virtual Trigger Trigger { get; set; }
    }
}