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
    internal sealed class FunctionList : IFunctionHandler
    {
        public FunctionTypeEnum FunctionType => FunctionTypeEnum.FunctionList;

        public void Handle(IModuleConfiguration moduleConfiguration, PiSensorNetDbContext context, Packet packet, IReadOnlyDictionary<string, IQueryableFunctionHandler> queryableFunctionHandlers, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> functions, Queue<Func<IHubProxy, Task>> hubTasksQueue)
        {
            var currentFunctions = (IReadOnlyDictionary<string, Function>)context.Functions
                                                                                 .ToDictionary(i => i.Name, i => i);
            var module = packet.Module;
            var moduleFunctions = context.ModuleFunctions
                                         .AsNoTracking()
                                         .Where(i => i.ModuleID == module.ID)
                                         .Select(i => i.Function.Name)
                                         .ToHashSet();

            var functionNames = packet.Text
                                      .ToLowerInvariant()
                                      .Split(moduleConfiguration.FunctionResultDelimiter)
                                      .Where(i => !string.IsNullOrEmpty(i));

            var newModuleFunctions = new Dictionary<FunctionTypeEnum, string>();
            foreach (var functionName in functionNames)
            {
                if (moduleFunctions.Contains(functionName))
                    continue;

                var function = currentFunctions.GetValueOrDefault(functionName)
                               ?? new Function(functionName, FunctionTypeEnum.Unknown, false);

                context.ModuleFunctions.Add(new ModuleFunction(module, function));
                
                newModuleFunctions.Add(function.FunctionType, function.Name);
            }

            if (newModuleFunctions.Count > 0)
            {
                context.SaveChanges();

                hubTasksQueue.Enqueue(proxy =>
                    proxy.Invoke("newModuleFunctions", module.ID, newModuleFunctions));
            }
        }
    }
}