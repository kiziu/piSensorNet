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
    public class Function : EntityBase
    {
        internal static void OnModelCreating(EntityTypeConfiguration<Function> entityTypeConfiguration)
        {
            entityTypeConfiguration.HasMany(i => i.ModuleFunctions)
                                   .WithRequired(i => i.Function);
        }

        protected Function() {}

        public Function(string name, FunctionTypeEnum functionType, bool isQueryable)
        {
            Name = name;
            FunctionType = functionType;
            IsQueryable = isQueryable;
        }


        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; protected internal set; }

        [Required]
        [Column(TypeName = "varchar")]
        [MaxLength(50)]
        public string Name { get; set; }

        [Column(TypeName = "text")]
        [MaxLength(500)]
        public string Description { get; set; } = null;

        public FunctionTypeEnum FunctionType { get; set; }

        public bool IsQueryable { get; set; }

        public DateTime Created { get; set; } = DateTime.Now;


        public virtual ICollection<ModuleFunction> ModuleFunctions { get; protected internal set; } = new List<ModuleFunction>();
    }
}