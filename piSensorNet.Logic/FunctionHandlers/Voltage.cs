using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using piSensorNet.Common;
using piSensorNet.DataModel.Context;
using piSensorNet.DataModel.Entities;
using piSensorNet.DataModel.Enums;
using piSensorNet.Logic.Custom;
using piSensorNet.Logic.FunctionHandlers.Base;

namespace piSensorNet.Logic.FunctionHandlers
{
    internal sealed class Voltage : IFunctionHandler
    {
        public FunctionTypeEnum FunctionType => FunctionTypeEnum.Voltage;

        public FunctionHandlerResult Handle(IModuleConfiguration moduleConfiguration, PiSensorNetDbContext context, Packet packet, IReadOnlyDictionary<string, IQueryableFunctionHandler> queryableFunctionHandlers, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> functions, ref Queue<Func<IHubProxy, Task>> hubTasksQueue)
        {
            if (packet.Module.State != ModuleStateEnum.Identified)
                return PacketStateEnum.Skipped;

            var reading = decimal.Parse(packet.Text);
            var module = packet.Module;

            var voltageReading = new VoltageReading(module.ID, reading, packet.Received);

            context.VoltageReadings.Add(voltageReading);
            context.SaveChanges();

            hubTasksQueue.Enqueue(proxy =>
                proxy.Invoke("newVoltageReading", module.ID, voltageReading.Value, voltageReading.Created, voltageReading.Received));

            return PacketStateEnum.Handled;
        }
    }
}
