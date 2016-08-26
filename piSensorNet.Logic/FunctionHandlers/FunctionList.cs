using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using piSensorNet.Common.Custom;
using piSensorNet.Common.Enums;
using piSensorNet.Common.Extensions;
using piSensorNet.DataModel.Entities;
using piSensorNet.Logic.Custom;
using piSensorNet.Logic.FunctionHandlers.Base;

namespace piSensorNet.Logic.FunctionHandlers
{
    internal sealed class FunctionList : FunctionHandlerBase
    {
        public override FunctionTypeEnum FunctionType => FunctionTypeEnum.FunctionList;

        public override FunctionHandlerResult Handle(FunctionHandlerContext context, Packet packet, ref HubMessageQueue hubMessageQueue)
        {
            var module = packet.Module;

            var currentFunctions = context.DatabaseContext
                                          .Functions
                                          .ToDictionary(i => i.Name, i => i)
                                          .ReadOnly();

            var currentModuleFunctions = context.DatabaseContext
                                         .ModuleFunctions
                                         .AsNoTracking()
                                         .Where(i => i.ModuleID == module.ID)
                                         .Select(i => i.FunctionID)
                                         .ToHashSet();

            var functionNames = packet.Text
                                      .ToLowerInvariant()
                                      .Split(context.ModuleConfiguration.FunctionResultDelimiter)
                                      .Where(i => !string.IsNullOrEmpty(i))
                                      .ToList();

            var newModuleFunctions = new List<KeyValuePair<FunctionTypeEnum, string>>(functionNames.Count);
            foreach (var functionName in functionNames)
            {
                var function = currentFunctions.GetValueOrDefault(functionName);
                if (function != null && currentModuleFunctions.Contains(function.ID))
                    continue;

                if (function == null)
                    Log(context, packet,
                        MethodBase.GetCurrentMethod().GetFullName(),
                        $"Unknown function '{functionName}' received in th list.");

                function = function ?? new Function(functionName, FunctionTypeEnum.Unknown, false);

                context.DatabaseContext.ModuleFunctions.Add(new ModuleFunction(module, function));

                newModuleFunctions.Add(KeyValuePair.Create(function.FunctionType, function.Name));
            }

            if (newModuleFunctions.Count > 0)
            {
                context.DatabaseContext.SaveChanges();

                hubMessageQueue.Enqueue(i => i.NewModuleFunctions(module.ID, newModuleFunctions));
            }

            return PacketStateEnum.Handled;
        }
    }
}