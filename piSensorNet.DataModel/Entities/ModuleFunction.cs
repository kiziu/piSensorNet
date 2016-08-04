using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using piSensorNet.DataModel.Entities.Base;

namespace piSensorNet.DataModel.Entities
{
    public class ModuleFunction : EntityBase
    {
        internal static void OnModelCreating(EntityTypeConfiguration<ModuleFunction> entityTypeConfiguration)
        {
            entityTypeConfiguration.HasRequired(i => i.Module)
                                   .WithMany(i => i.ModuleFunctions)
                                   .HasForeignKey(i => i.ModuleID);

            entityTypeConfiguration.HasRequired(i => i.Function)
                                   .WithMany(i => i.ModuleFunctions)
                                   .HasForeignKey(i => i.FunctionID);
        }

        protected ModuleFunction() {}

        public ModuleFunction(int moduleID, int functionID)
        {
            ModuleID = moduleID;
            FunctionID = functionID;
        }

        public ModuleFunction(Module module, Function function)
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            Module = module;
            // ReSharper disable once VirtualMemberCallInConstructor
            Function = function;
        }


        [Key]
        [Column(Order = 0)]
        public int ModuleID { get; set; }

        [Key]
        [Column(Order = 1)]
        public int FunctionID { get; set; }


        public virtual Module Module { get; set; }

        public virtual Function Function { get; set; }
    }
}