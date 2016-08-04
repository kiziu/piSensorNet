using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using piSensorNet.DataModel.Entities.Base;

namespace piSensorNet.DataModel.Entities
{
    public class TemperatureReading : EntityBase
    {
        internal static void OnModelCreating(EntityTypeConfiguration<TemperatureReading> entityTypeConfiguration)
        {
            entityTypeConfiguration.Property(i => i.Value).HasPrecision(5, 2);

            entityTypeConfiguration.HasRequired(i => i.TemperatureSensor)
                                   .WithMany(i => i.TemperatureReadings)
                                   .HasForeignKey(i => i.TemperatureSensorID);
        }

        protected TemperatureReading() {}

        public TemperatureReading(int temperatureSensorID, decimal value, DateTime received)
        {
            TemperatureSensorID = temperatureSensorID;
            Value = value;
            Received = received;
        }

        public TemperatureReading(TemperatureSensor temperatureSensor, decimal value, DateTime received)
        {
            Value = value;
            Received = received;
            // ReSharper disable once VirtualMemberCallInConstructor
            TemperatureSensor = temperatureSensor;
        }


        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; protected internal set; }

        public int TemperatureSensorID { get; set; }

        public decimal Value { get; set; }

        public DateTime Created { get; set; } = DateTime.Now;

        public DateTime Received { get; set; }


        public virtual TemperatureSensor TemperatureSensor { get; set; }
    }
}