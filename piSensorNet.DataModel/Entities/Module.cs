using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using piSensorNet.Common.Enums;
using piSensorNet.DataModel.Entities.Base;

namespace piSensorNet.DataModel.Entities
{
    public class Module : EntityBase<Module>
    {
        internal static void OnModelCreating(EntityTypeConfiguration<Module> entityTypeConfiguration)
        {
            entityTypeConfiguration.HasMany(i => i.Packets)
                                   .WithRequired(i => i.Module);

            entityTypeConfiguration.HasMany(i => i.Messages)
                                   .WithOptional(i => i.Module);

            entityTypeConfiguration.HasMany(i => i.TemperatureSensors)
                                   .WithRequired(i => i.Module);

            entityTypeConfiguration.HasMany(i => i.VoltageReadings)
                                   .WithRequired(i => i.Module);

            entityTypeConfiguration.HasMany(i => i.ModuleFunctions)
                                   .WithRequired(i => i.Module);
        }

        protected Module() {}

        public Module(string address)
        {
            Address = address;
        }


        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; protected internal set; }

        [Column(TypeName = "varchar")]
        [MaxLength(50)]
        public string FriendlyName { get; set; } = null;

        [Column(TypeName = "varchar")]
        [MaxLength(500)]
        public string Description { get; set; } = null;

        [Required]
        [Column(TypeName = "char")]
        [MaxLength(5)]
        public string Address { get; set; }

        public ModuleStateEnum State { get; set; } = ModuleStateEnum.New;

        public DateTime Created { get; set; } = DateTime.Now;


        public virtual ICollection<Packet> Packets { get; protected internal set; } = new List<Packet>();

        public virtual ICollection<Message> Messages { get; protected internal set; } = new List<Message>();

        public virtual ICollection<TemperatureSensor> TemperatureSensors { get; protected internal set; } = new List<TemperatureSensor>();

        public virtual ICollection<VoltageReadout> VoltageReadings { get; protected internal set; } = new List<VoltageReadout>();

        public virtual ICollection<ModuleFunction> ModuleFunctions { get; protected internal set; } = new List<ModuleFunction>();
    }
}
