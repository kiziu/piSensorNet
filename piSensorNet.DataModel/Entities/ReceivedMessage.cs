using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using piSensorNet.DataModel.Entities.Base;

namespace piSensorNet.DataModel.Entities
{
    public class ReceivedMessage : EntityBase
    {       
        internal static void OnModelCreating(EntityTypeConfiguration<ReceivedMessage> entityTypeConfiguration)
        {
            entityTypeConfiguration.HasMany(i => i.ResultMessages)
                                   .WithOptional(i => i.ResultMessage);
        }

        protected ReceivedMessage() {}

        public ReceivedMessage(string text, DateTime received, bool? isFailed, bool isPacket)
        {
            Text = text;
            Received = received;
            IsFailed = isFailed;
            IsPacket = isPacket;
        }


        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; protected internal set; }
        
        [Required]
        [Column(TypeName = "varchar")]
        [MaxLength(100)]
        public string Text { get; set; }

        public bool? IsFailed { get; set; }

        public bool IsPacket { get; set; }

        public bool HasPartialPacket { get; set; } = false;
        
        public DateTime Received { get; set; }


        public virtual PartialPacket PartialPacket { get; set; }


        public virtual ICollection<Message> ResultMessages { get; protected internal set; } = new List<Message>();
    }
}