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
        internal static void OnModelCreating(EntityTypeConfiguration<Trigger> entityTypeConfiguration) {}

        protected Trigger() {}

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

        [Column(TypeName = "varchar")]
        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        [Column(TypeName = "varchar")]
        [MaxLength(5000)]
        public string Content { get; set; }

        public DateTime LastModified { get; set; }

        public DateTime Created { get; set; } = DateTime.Now;


        public virtual ICollection<TriggerSource> TriggerSources { get; protected internal set; } = new List<TriggerSource>();

        public virtual ICollection<TriggerDependency> TriggerDependencies { get; protected internal set; } = new List<TriggerDependency>();
    }
}
