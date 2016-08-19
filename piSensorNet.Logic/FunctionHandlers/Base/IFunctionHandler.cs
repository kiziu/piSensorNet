using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using piSensorNet.Common.Enums;
using piSensorNet.DataModel.Entities;
using piSensorNet.Logic.Custom;

namespace piSensorNet.Logic.FunctionHandlers.Base
{
    public interface IFunctionHandler
    {
        FunctionTypeEnum FunctionType { get; }
        bool IsModuleIdentityRequired { get; }

        [CanBeNull]
        TriggerSourceTypeEnum? TriggerSourceType { get; }

        FunctionHandlerResult Handle([NotNull] FunctionHandlerContext context, [NotNull] Packet packet, [NotNull] ref Queue<Action<IMainHubEngine>> hubMessageQueue);
    }
}