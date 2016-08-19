using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using piSensorNet.Common.Enums;
using piSensorNet.DataModel.Entities;

namespace piSensorNet.Logic.FunctionHandlers.Base
{
    public interface IQueryableFunctionHandler : IFunctionHandler
    {
        PacketStateEnum Handle([NotNull] FunctionHandlerContext context, [NotNull] Packet originalPacket, [NotNull] string response, [NotNull] Queue<Action<IMainHubEngine>> hubMessageQueue);
    }
}