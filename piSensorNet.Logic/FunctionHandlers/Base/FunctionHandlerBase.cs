using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.Common.Enums;
using piSensorNet.DataModel.Entities;
using piSensorNet.Logic.Custom;

namespace piSensorNet.Logic.FunctionHandlers.Base
{
    internal abstract class FunctionHandlerBase : IFunctionHandler
    {
        public abstract FunctionTypeEnum FunctionType { get; }

        public virtual bool IsModuleIdentityRequired => true;
        public virtual TriggerSourceTypeEnum? TriggerSourceType => null;

        public abstract FunctionHandlerResult Handle(FunctionHandlerContext context, Packet packet, ref Queue<Action<IMainHubEngine>> hubMessageQueue);

        protected PacketStateEnum LogAndReturn(FunctionHandlerContext context, Packet packet, string source, string text)
        {
            Log(context, packet, source, text);

            context.DatabaseContext.SaveChanges();

            return PacketStateEnum.Failed;

        }

        protected void Log(FunctionHandlerContext context, Packet packet, string source, string text)
        {
            var entity = new Log(source, text)
                         {
                             PacketID = packet.ID,
                         };

            context.DatabaseContext
                   .Logs
                   .Add(entity);
        }
    }
}
