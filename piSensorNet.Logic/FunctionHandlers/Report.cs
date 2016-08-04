using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using piSensorNet.Common;
using piSensorNet.Common.Extensions;
using piSensorNet.DataModel.Context;
using piSensorNet.DataModel.Entities;
using piSensorNet.DataModel.Enums;
using piSensorNet.Logic.FunctionHandlers.Base;

namespace piSensorNet.Logic.FunctionHandlers
{
    internal sealed class Report : IFunctionHandler
    {
        public FunctionTypeEnum FunctionType => FunctionTypeEnum.Report;

        public void Handle(IModuleConfiguration moduleConfiguration, PiSensorNetDbContext context, Packet packet, IReadOnlyDictionary<string, IQueryableFunctionHandler> queryableFunctionHandlers, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> functions, Queue<Func<IHubProxy, Task>> hubTasksQueue)
        {
            var queryableFunctionPairs = FunctionHandlerHelper.SplitPairs(packet.Text, moduleConfiguration.FunctionResultDelimiter, moduleConfiguration.FunctionResultNameDelimiter);
            var module = packet.Module;
            var moduleFunctions = context.ModuleFunctions
                                         .AsNoTracking()
                                         .Where(i => i.ModuleID == module.ID)
                                         .Select(i => i.Function.Name)
                                         .ToHashSet();

            foreach (var queryableFunctionPair in queryableFunctionPairs)
            {
                if (!moduleFunctions.Contains(queryableFunctionPair.Key))
                    continue;

                var handler = queryableFunctionHandlers.GetValueOrDefault(queryableFunctionPair.Key);

                handler?.Handle(moduleConfiguration, context, packet, queryableFunctionPair.Value, hubTasksQueue);
            }
        }
    }
}