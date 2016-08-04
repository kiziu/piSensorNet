using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using piSensorNet.DataModel.Entities.Base;
using piSensorNet.DataModel.Enums;

namespace piSensorNet.DataModel.Entities
{
    public class PartialPacket : EntityBase<PartialPacket>
    {
        internal static void OnModelCreating(EntityTypeConfiguration<PartialPacket> entityTypeConfiguration)
        {
            entityTypeConfiguration.HasOptional(i => i.Packet)
                                   .WithMany(i => i.PartialPackets)
                                   .HasForeignKey(i => i.PacketID);
        }

        protected PartialPacket() {}

        public PartialPacket(string address, byte number, byte current, byte total, string message, DateTime received)
        {
            Address = address;
            Number = number;
            Current = current;
            Total = total;
            Message = message;
            Received = received;
        }


        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int? PacketID { get; set; } = null;

        [Required]
        [Column(TypeName = "char")]
        [MaxLength(5)]
        public string Address { get; set; }

        public byte Number { get; set; }

        public byte Current { get; set; }

        public byte Total { get; set; }

        [Column(TypeName = "char")]
        [MaxLength(32)]
        public string Message { get; set; }

        public PartialPacketStateEnum State { get; set; } = PartialPacketStateEnum.New;

        public DateTime Created { get; set; } = DateTime.Now;

        public DateTime Received { get; set; }


        public virtual Packet Packet { get; set; }
    }
}