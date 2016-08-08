using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using piSensorNet.DataModel.Entities.Base;

namespace piSensorNet.DataModel.Entities
{
    public class Trigger : EntityBase
    {
        internal static void OnModelCreating(EntityTypeConfiguration<Trigger> entityTypeConfiguration)
        {
        }

        protected Trigger() { }

        public Trigger(string friendlyName, string content, string description)
        {
            FriendlyName = friendlyName;
            Content = content;
            Description = description;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; protected internal set; }

        [Required]
        [Column(TypeName = "varchar")]
        [MaxLength(50)]
        public string FriendlyName { get; set; }

        [Required]
        [Column(TypeName = "text")]
        [MaxLength(5000)]
        public string Content { get; set; }

        [Column(TypeName = "text")]
        [MaxLength(500)]
        public string Description { get; set; }
        
        public DateTime Created { get; set; } = DateTime.Now;

        //public virtual ICollection<>  { get; protected internal set; } = new List<>();
    }
}
