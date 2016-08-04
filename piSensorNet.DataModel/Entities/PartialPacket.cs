using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using piSensorNet.DataModel.Entities.Base;

namespace piSensorNet.DataModel.Entities
{
    public class PartialPacket : EntityBase
    {
        internal static void OnModelCreating(EntityTypeConfiguration<PartialPacket> entityTypeConfiguration)
        {
            entityTypeConfiguration.HasOptional(i => i.Packet)
                                   .WithMany(i => i.PartialPackets)
                                   .HasForeignKey(i => i.PacketID);

            entityTypeConfiguration.HasRequired(i => i.ReceivedMessage)
                                   .WithOptional(i => i.PartialPacket);
        }

        protected PartialPacket() {}

        public PartialPacket(int receivedMessageID, string address, byte number, byte current, byte total, string message, DateTime received)
        {
            ReceivedMessageID = receivedMessageID;
            Address = address;
            Number = number;
            Current = current;
            Total = total;
            Message = message;
            Received = received;
        }

        public PartialPacket(ReceivedMessage receivedMessage, string address, byte number, byte current, byte total, string message, DateTime received)
        {
            Address = address;
            Number = number;
            Current = current;
            Total = total;
            Message = message;
            ReceivedMessage = receivedMessage;
            Received = received;
        }


        [Key]
        public int ReceivedMessageID { get; set; }

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

        public DateTime Created { get; set; } = DateTime.Now;

        public DateTime Received { get; set; }


        public virtual Packet Packet { get; set; }

        public virtual ReceivedMessage ReceivedMessage { get; set; }
    }
}