using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.Common;
using piSensorNet.Common.Extensions;
using piSensorNet.DataModel.Context;
using piSensorNet.DataModel.Entities;
using piSensorNet.DataModel.Enums;
using piSensorNet.Logic.Custom;
using piSensorNet.Logic.FunctionHandlers.Base;

namespace piSensorNet.Logic.FunctionHandlers
{
    internal sealed class FunctionList : IFunctionHandler
    {
        public FunctionTypeEnum FunctionType => FunctionTypeEnum.FunctionList;

        public FunctionHandlerResult Handle(IModuleConfiguration moduleConfiguration, PiSensorNetDbContext context, Packet packet, IReadOnlyDictionary<string, IQueryableFunctionHandler> queryableFunctionHandlers, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> functions, ref Queue<Action<IMainHubEngine>> hubMessageQueue)
        {
            if (packet.Module.State != ModuleStateEnum.Identified)
                return PacketStateEnum.Skipped;

            var currentFunctions = context.Functions
                                          .ToDictionary(i => i.Name, i => i)
                                          .ReadOnly();

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

            var newModuleFunctions = new List<FunctionTypeEnum>();
            foreach (var functionName in functionNames)
            {
                if (moduleFunctions.Contains(functionName))
                    continue;

                var function = currentFunctions.GetValueOrDefault(functionName)
                               ?? new Function(functionName, FunctionTypeEnum.Unknown, false);

                context.ModuleFunctions.Add(new ModuleFunction(module, function));

                newModuleFunctions.Add(function.FunctionType);
            }

            if (newModuleFunctions.Count > 0)
            {
                context.SaveChanges();

                hubMessageQueue.Enqueue(proxy => proxy.NewModuleFunctions(module.ID, newModuleFunctions));
            }

            return PacketStateEnum.Handled;
        }
    }
}