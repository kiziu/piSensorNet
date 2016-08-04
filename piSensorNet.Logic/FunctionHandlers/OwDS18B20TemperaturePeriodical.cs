using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using piSensorNet.Common;
using piSensorNet.Common.Extensions;
using piSensorNet.DataModel.Context;
using piSensorNet.DataModel.Entities;
using piSensorNet.DataModel.Enums;
using piSensorNet.DataModel.Extensions;
using piSensorNet.Logic.FunctionHandlers.Base;

namespace piSensorNet.Logic.FunctionHandlers
{
    internal sealed class OwDS18B20TemperaturePeriodical : IQueryableFunctionHandler
    {
        public FunctionTypeEnum FunctionType => FunctionTypeEnum.OwDS18B20TemperaturePeriodical;

        public void Handle(IModuleConfiguration moduleConfiguration, PiSensorNetDbContext context, Packet packet, IReadOnlyDictionary<string, IQueryableFunctionHandler> queryableFunctionHandlers, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> functions, Queue<Func<IHubProxy, Task>> hubTasksQueue) 
            => Handle(moduleConfiguration, context, packet, packet.Text, hubTasksQueue);

        public void Handle(IModuleConfiguration moduleConfiguration, PiSensorNetDbContext context, Packet originalPacket, string response, Queue<Func<IHubProxy, Task>> hubTasksQueue)
        {
            var module = originalPacket.Module;
            var periodUnits = int.Parse(response);
            var periodLengthinMs = periodUnits * moduleConfiguration.PeriodUnitLengthInMs;
            var period = TimeSpan.FromMilliseconds(periodLengthinMs);

            context.Database.ExecuteSqlCommand(TemperatureSensor.GenerateUpdate(
                context.GetTableName<TemperatureSensor>(),
                new Dictionary<Expression<Func<TemperatureSensor, object>>, string>
                {
                    {i => i.Period, period.ToSql()}
                },
                new KeyValuePair<Expression<Func<TemperatureSensor, object>>, string>(i => i.ModuleID, module.ID.ToSql())));

            hubTasksQueue.Enqueue(proxy => proxy.Invoke("changedTemperatureSensorPeriod", module.ID, period));
        }
    }
}