using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using piSensorNet.DataModel.Entities.Base;

namespace piSensorNet.DataModel.Entities
{
    public class VoltageReadout : EntityBase
    {
        internal static void OnModelCreating(EntityTypeConfiguration<VoltageReadout> entityTypeConfiguration)
        {
            entityTypeConfiguration.Property(i => i.Value).HasPrecision(5, 3);

            entityTypeConfiguration.HasRequired(i => i.Module)
                                   .WithMany(i => i.VoltageReadings)
                                   .HasForeignKey(i => i.ModuleID);
        }

        protected VoltageReadout() {}

        public VoltageReadout(int moduleID, decimal value, DateTime received)
        {
            ModuleID = moduleID;
            Value = value;
            Received = received;
        }

        public VoltageReadout(Module module, decimal value, DateTime received)
        {
            Value = value;
            Received = received;
            // ReSharper disable once VirtualMemberCallInConstructor
            Module = module;
        }


        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; protected internal set; }

        public int ModuleID { get; set; }

        public decimal Value { get; set; }

        public DateTime Created { get; set; } = DateTime.Now;

        public DateTime Received { get; set; }


        public virtual Module Module { get; set; }
    }
}