using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using piSensorNet.Common;
using piSensorNet.DataModel.Context;
using piSensorNet.DataModel.Entities;
using piSensorNet.DataModel.Enums;
using piSensorNet.DataModel.Extensions;
using piSensorNet.Logic.Custom;
using piSensorNet.Logic.FunctionHandlers.Base;

namespace piSensorNet.Logic.FunctionHandlers
{
    internal sealed class OwDS18B20TemperaturePeriodical : IQueryableFunctionHandler
    {
        public FunctionTypeEnum FunctionType => FunctionTypeEnum.OwDS18B20TemperaturePeriodical;

        public FunctionHandlerResult Handle(IModuleConfiguration moduleConfiguration, PiSensorNetDbContext context, Packet packet, IReadOnlyDictionary<string, IQueryableFunctionHandler> queryableFunctionHandlers, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> functions, ref Queue<Func<IHubProxy, Task>> hubTasksQueue)
        {
            if (packet.Module.State != ModuleStateEnum.Identified)
                return PacketStateEnum.Skipped;

            Handle(moduleConfiguration, context, packet, packet.Text, hubTasksQueue);

            return PacketStateEnum.Handled;
        }

        public void Handle(IModuleConfiguration moduleConfiguration, PiSensorNetDbContext context, Packet originalPacket, string response, Queue<Func<IHubProxy, Task>> hubTasksQueue)
        {
            var module = originalPacket.Module;
            var periodUnits = int.Parse(response);
            var periodLengthinMs = periodUnits * moduleConfiguration.PeriodUnitLengthInMs;
            var period = TimeSpan.FromMilliseconds(periodLengthinMs);

            context.EnqueueQuery(TemperatureSensor.GenerateUpdate(
                context,
                new Dictionary<Expression<Func<TemperatureSensor, object>>, string>
                {
                    {i => i.Period, period.ToSql()}
                },
                new Tuple<Expression<Func<TemperatureSensor, object>>, string, string>(i => i.ModuleID, "=", module.ID.ToSql())));

            context.ExecuteQueries();

            hubTasksQueue.Enqueue(proxy => proxy.Invoke("changedTemperatureSensorPeriod", module.ID, period));
        }
    }
}