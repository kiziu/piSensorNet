using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using piSensorNet.DataModel.Entities.Base;

namespace piSensorNet.DataModel.Entities
{
    public class Log : EntityBase
    {
        internal static void OnModelCreating(EntityTypeConfiguration<Log> entityTypeConfiguration)
        {
            entityTypeConfiguration.HasOptional(i => i.Message)
                                   .WithMany()
                                   .HasForeignKey(i => i.MessageID);

            entityTypeConfiguration.HasOptional(i => i.Packet)
                                   .WithMany()
                                   .HasForeignKey(i => i.PacketID);

            entityTypeConfiguration.HasOptional(i => i.Module)
                                   .WithMany()
                                   .HasForeignKey(i => i.ModuleID);
        }

        protected Log() {}

        public Log(string source, string text)
        {
            Source = source;
            Text = text;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; protected internal set; }

        [Required]
        [Column(TypeName = "varchar")]
        [MaxLength(100)]
        public string Source { get; set; }

        [Required]
        [Column(TypeName = "varchar")]
        [MaxLength(1000)]
        public string Text { get; set; }


        public int? MessageID { get; set; } = null;
        public int? PacketID { get; set; } = null;
        public int? ModuleID { get; set; } = null;

        public DateTime Created { get; set; } = DateTime.Now;

        public virtual Message Message { get; set; }
        public virtual Packet Packet { get; set; }
        public virtual Module Module { get; set; }
    }
}