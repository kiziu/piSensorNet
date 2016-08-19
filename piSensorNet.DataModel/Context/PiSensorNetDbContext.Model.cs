using System;
using System.Data.Entity;
using System.Linq;
using piSensorNet.DataModel.Entities;

namespace piSensorNet.DataModel.Context
{
    public partial class PiSensorNetDbContext
    {
        public DbSet<Log> Logs { get; set; }

        public DbSet<Message> Messages { get; set; }
               
        public DbSet<PartialPacket> PartialPackets { get; set; }
        public DbSet<Packet> Packets { get; set; }
               
        public DbSet<Module> Modules { get; set; }
        public DbSet<Function> Functions { get; set; }
        public DbSet<ModuleFunction> ModuleFunctions { get; set; }

        public DbSet<VoltageReadout> VoltageReadouts { get; set; }
               
        public DbSet<TemperatureSensor> TemperatureSensors { get; set; }
        public DbSet<TemperatureReadout> TemperatureReadouts { get; set; }

        public DbSet<Trigger> Triggers { get; set; }
        public DbSet<TriggerSource> TriggerSources { get; set; }
        public DbSet<TriggerDependency> TriggerDependencies { get; set; }
    }
}