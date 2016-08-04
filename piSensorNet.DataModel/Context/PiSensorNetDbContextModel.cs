using System;
using System.Data.Entity;
using System.Linq;
using piSensorNet.DataModel.Entities;

namespace piSensorNet.DataModel.Context
{
    public partial class PiSensorNetDbContext
    {
        public DbSet<ReceivedMessage> ReceivedMessages { get; set; }
        //public DbSet<SentMessage> SentMessages { get; set; }

        public DbSet<Message> Messages { get; set; }

        public DbSet<PartialPacket> PartialPackets { get; set; }
        public DbSet<Packet> Packets { get; set; }

        public DbSet<Module> Modules { get; set; }
        public DbSet<ModuleFunction> ModuleFunctions { get; set; }
        public DbSet<Function> Functions { get; set; }

        public DbSet<VoltageReading> VoltageReadings { get; set; }

        public DbSet<TemperatureSensor> TemperatureSensors { get; set; }
        public DbSet<TemperatureReading> TemperatureReadings { get; set; }
    }
}