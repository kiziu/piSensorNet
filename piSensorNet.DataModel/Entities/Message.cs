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
    public class Message : EntityBase
    {
        internal static void OnModelCreating(EntityTypeConfiguration<Message> entityTypeConfiguration)
        {
            entityTypeConfiguration.HasOptional(i => i.Module)
                                   .WithMany(i => i.Messages)
                                   .HasForeignKey(i => i.ModuleID);
            
            entityTypeConfiguration.HasRequired(i => i.Function)
                                   .WithMany()
                                   .HasForeignKey(i => i.FunctionID);
            
            entityTypeConfiguration.HasMany(i => i.Packets)
                                   .WithOptional(i => i.Message);
        }

        protected Message() {}

        public Message(int functionID, bool isQuery)
        {
            FunctionID = functionID;
            IsQuery = isQuery;
        }

        public Message(Function function, bool isQuery)
        {
            IsQuery = isQuery;
            // ReSharper disable once VirtualMemberCallInConstructor
            Function = function;
        }


        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; protected internal set; }

        public int? ModuleID { get; set; } = null;

        public int FunctionID { get; set; }
        
        [Column(TypeName = "varchar")]
        [MaxLength(500)]
        public string Text { get; set; } = null;

        public bool IsQuery { get; set; }

        public DateTime Created { get; set; } = DateTime.Now;

        public MessageStateEnum State { get; set; } = MessageStateEnum.Queued;

        public DateTime? Sent { get; set; } = null;

        public DateTime? ResponseReceived { get; set; } = null;

        [Column(TypeName = "varchar")]
        [MaxLength(500)]
        public string Error { get; set; }


        public virtual Module Module { get; set; }

        public virtual Function Function { get; set; }


        public virtual ICollection<Packet> Packets { get; protected internal set; } = new List<Packet>();
    }
}