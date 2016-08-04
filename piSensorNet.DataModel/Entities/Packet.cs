﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using piSensorNet.DataModel.Entities.Base;
using piSensorNet.DataModel.Enums;

namespace piSensorNet.DataModel.Entities
{
    public class Packet : EntityBase<Packet>
    {
        internal static void OnModelCreating(EntityTypeConfiguration<Packet> entityTypeConfiguration)
        {
            entityTypeConfiguration.HasRequired(i => i.Module)
                                   .WithMany(i => i.Packets)
                                   .HasForeignKey(i => i.ModuleID);

            entityTypeConfiguration.HasOptional(i => i.Message)
                                   .WithMany(i => i.Packets)
                                   .HasForeignKey(i => i.MessageID);

            entityTypeConfiguration.HasOptional(i => i.Function)
                                   .WithMany()
                                   .HasForeignKey(i => i.FunctionID);

            entityTypeConfiguration.HasMany(i => i.PartialPackets)
                                   .WithOptional(i => i.Packet);
        }

        protected Packet() {}

        public Packet(int moduleID, byte number, string text, DateTime received)
        {
            ModuleID = moduleID;
            Number = number;
            Text = text;
            Received = received;
        }

        public Packet(Module module, byte number, string text, DateTime received)
        {
            Number = number;
            Text = text;
            Received = received;
            Module = module;
        }


        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; protected internal set; }

        public int ModuleID { get; set; }

        public int? MessageID { get; set; } = null;

        public int? FunctionID { get; set; } = null;

        public byte Number { get; set; }

        public PacketStateEnum State { get; set; } = PacketStateEnum.New;

        [Required]
        [Column(TypeName = "text")]
        public string Text { get; set; }

        public DateTime Created { get; set; } = DateTime.Now;

        public DateTime Received { get; set; }

        public DateTime? Handled { get; set; } = null;


        public virtual Module Module { get; set; }

        public virtual Message Message { get; set; }

        public virtual Function Function { get; set; }


        public virtual ICollection<PartialPacket> PartialPackets { get; protected internal set; } = new List<PartialPacket>();
    }
}
