using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using piSensorNet.DataModel.Entities.Base;

namespace piSensorNet.DataModel.Entities
{
    public class TemperatureSensor : EntityBase<TemperatureSensor>
    {
        internal static void OnModelCreating(EntityTypeConfiguration<TemperatureSensor> entityTypeConfiguration)
        {
            entityTypeConfiguration.Property(i => i.Period).HasPrecision(1);

            entityTypeConfiguration.HasRequired(i => i.Module)
                                   .WithMany(i => i.TemperatureSensors)
                                   .HasForeignKey(i => i.ModuleID);

            entityTypeConfiguration.HasMany(i => i.TemperatureReadings)
                                   .WithRequired(i => i.TemperatureSensor);
        }

        protected TemperatureSensor() {}

        public TemperatureSensor(int moduleID, string address)
        {
            ModuleID = moduleID;
            Address = address;
        }

        public TemperatureSensor(Module module, string address)
        {
            Address = address;
            Module = module;
        }


        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; protected internal set; }

        public int ModuleID { get; set; }

        [Required]
        [Column(TypeName = "varchar")]
        [MaxLength(50)]
        public string Address { get; set; }

        [Column(TypeName = "varchar")]
        [MaxLength(50)]
        public string FriendlyName { get; set; } = null;

        public TimeSpan? Period { get; set; } = null;

        public DateTime Created { get; set; } = DateTime.Now;


        public virtual Module Module { get; set; }


        public virtual ICollection<TemperatureReading> TemperatureReadings { get; protected internal set; } = new List<TemperatureReading>();
    }
}