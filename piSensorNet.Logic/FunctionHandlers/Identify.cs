using System;
using System.Collections.Generic;
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

        public override FunctionHandlerResult Handle(FunctionHandlerContext context, Packet packet, ref Queue<Action<IMainHubEngine>> hubMessageQueue)
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
            
            //context.EnqueueRaw(Module.GenerateUpdate(context,
            //    new Dictionary<Expression<Func<Module, object>>, string>
            //    {
            //        {i => i.State, ModuleStateEnum.Identified.ToSql()},
            //    },
            //    new Tuple<Expression<Func<Module, object>>, string, string>(i => i.ID, "=", packet.ModuleID.ToSql())));

            context.DatabaseContext.EnqueueUpdate<Module>(
                i => i.State == ModuleStateEnum.Identified,
                i => i.ID == packet.ModuleID);

            //context.EnqueueRaw(Packet.GenerateUpdate(context,
            //    new Dictionary<Expression<Func<Packet, object>>, string>
            //    {
            //        {i => i.State, PacketStateEnum.New.ToSql()},
            //    },
            //    new Tuple<Expression<Func<Packet, object>>, string, string>(i => i.ModuleID, "=", packet.ModuleID.ToSql()),
            //    new Tuple<Expression<Func<Packet, object>>, string, string>(i => i.State, "=", PacketStateEnum.Skipped.ToSql())));

            context.DatabaseContext.EnqueueUpdate<Packet>(
                i => i.State == PacketStateEnum.New,
                i => i.ModuleID == packet.ModuleID && i.State == PacketStateEnum.Skipped);

            context.DatabaseContext.SaveChanges();

            hubMessageQueue.Enqueue(hubProxy => hubProxy.NewModule(module.ID, module.Address));

            return new FunctionHandlerResult(PacketStateEnum.Handled, true, true);
        }
    }
}