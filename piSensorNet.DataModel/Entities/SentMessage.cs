using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using piSensorNet.DataModel.Entities.Base;

namespace piSensorNet.DataModel.Entities
{
    internal class SentMessage : EntityBase<SentMessage>
    {
        internal static void OnModelCreating(EntityTypeConfiguration<SentMessage> entityTypeConfiguration)
        {
            //entityTypeConfiguration.HasRequired(i => i.Message)
            //                       .WithOptional(i => i.SentMessage);
        }

        protected SentMessage() {}

        public SentMessage(int messageID, string text)
        {
            MessageID = messageID;
            Text = text;
        }

        public SentMessage(Message message, string text)
        {
            Text = text;
            Message = message;
        }


        [Key]
        public int MessageID { get; set; }

        [Required]
        [Column(TypeName = "varchar")]
        [MaxLength(500)]
        public string Text { get; set; }
        
        public DateTime Created { get; set; } = DateTime.Now;

        public DateTime? Sent { get; set; } = null;


        public virtual Message Message { get; set; }
    }
}