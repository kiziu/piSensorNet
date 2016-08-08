using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.Common;
using piSensorNet.DataModel.Context;
using piSensorNet.DataModel.Entities;

namespace piSensorNet.Logic.FunctionHandlers.Base
{
    public interface IQueryableFunctionHandler : IFunctionHandler
    {
        void Handle(IModuleConfiguration moduleConfiguration, PiSensorNetDbContext context, Packet originalPacket, string response, Queue<Action<IMainHubEngine>> hubMessageQueue);
    }
}