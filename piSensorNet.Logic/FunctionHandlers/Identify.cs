using System;
using System.Linq;
using piSensorNet.Common.Enums;
using piSensorNet.DataModel.Entities;
using piSensorNet.Logic.Custom;
using piSensorNet.Logic.FunctionHandlers.Base;

namespace piSensorNet.Logic.FunctionHandlers
{
    internal sealed class Identify : FunctionHandlerBase
    {
        public override FunctionTypeEnum FunctionType => FunctionTypeEnum.Identify;
        public override bool IsModuleIdentityRequired => false;

        public override FunctionHandlerResult Handle(FunctionHandlerContext context, Packet packet, ref HubMessageQueue hubMessageQueue)
        {
            var module = packet.Module;
            if (module.State != ModuleStateEnum.New)
                return PacketStateEnum.Redundant;

            context.DatabaseContext.Messages.Add(new Message(context.FunctionTypes.Forward[FunctionTypeEnum.FunctionList], false)
                                 {
                                     Module = module
                                 });

            context.DatabaseContext.Messages.Add(new Message(context.FunctionTypes.Forward[FunctionTypeEnum.Report], false)
                                 {
                                     Module = module
                                 });

            module.State = ModuleStateEnum.Identified;
            
            context.DatabaseContext.EnqueueUpdate<Module>(
                i => i.State == ModuleStateEnum.Identified,
                i => i.ID == packet.ModuleID);
            
            context.DatabaseContext.EnqueueUpdate<Packet>(
                i => i.State == PacketStateEnum.New,
                i => i.ModuleID == packet.ModuleID && i.State == PacketStateEnum.Skipped);

            context.DatabaseContext.SaveChanges();

            hubMessageQueue.Enqueue(i => i.NewModule(module.ID, module.Address));

            return new FunctionHandlerResult(PacketStateEnum.Handled, true, true);
        }
    }
}