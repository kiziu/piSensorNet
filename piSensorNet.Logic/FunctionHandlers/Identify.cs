using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using piSensorNet.Common;
using piSensorNet.DataModel.Context;
using piSensorNet.DataModel.Entities;
using piSensorNet.DataModel.Enums;
using piSensorNet.DataModel.Extensions;
using piSensorNet.Logic.Custom;
using piSensorNet.Logic.FunctionHandlers.Base;

namespace piSensorNet.Logic.FunctionHandlers
{
    internal sealed class Identify : IFunctionHandler
    {
        public FunctionTypeEnum FunctionType => FunctionTypeEnum.Identify;

        public FunctionHandlerResult Handle(IModuleConfiguration moduleConfiguration, PiSensorNetDbContext context, Packet packet, IReadOnlyDictionary<string, IQueryableFunctionHandler> queryableFunctionHandlers, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> functions, ref Queue<Action<IMainHubEngine>> hubMessageQueue)
        {
            var module = packet.Module;
            if (module.State != ModuleStateEnum.New)
                return PacketStateEnum.Redundant;

            context.Messages.Add(new Message(functions[FunctionTypeEnum.FunctionList].Key, false)
                                 {
                                     Module = module
                                 });

            context.Messages.Add(new Message(functions[FunctionTypeEnum.Report].Key, false)
                                 {
                                     Module = module
                                 });

            module.State = ModuleStateEnum.Identified;

            context.EnqueueRaw(Module.GenerateUpdate(context,
                new Dictionary<Expression<Func<Module, object>>, string>
                {
                    {i => i.State, ModuleStateEnum.Identified.ToSql()},
                },
                new Tuple<Expression<Func<Module, object>>, string, string>(i => i.ID, "=", packet.ModuleID.ToSql())));

            context.EnqueueRaw(Packet.GenerateUpdate(context,
                new Dictionary<Expression<Func<Packet, object>>, string>
                {
                    {i => i.State, PacketStateEnum.New.ToSql()},
                },
                new Tuple<Expression<Func<Packet, object>>, string, string>(i => i.ModuleID, "=", packet.ModuleID.ToSql()),
                new Tuple<Expression<Func<Packet, object>>, string, string>(i => i.State, "=", PacketStateEnum.Skipped.ToSql())));
            
            context.SaveChanges();

            hubMessageQueue.Enqueue(hubProxy => hubProxy.NewModule(module.ID, module.Address));

            return new FunctionHandlerResult(PacketStateEnum.Handled, true, true);
        }
    }
}