using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using piSensorNet.Common.Enums;
using piSensorNet.Common.Extensions;
using piSensorNet.DataModel.Entities;
using piSensorNet.Logic.Custom;
using piSensorNet.Logic.FunctionHandlers.Base;

namespace piSensorNet.Logic.FunctionHandlers
{
    internal sealed class Report : FunctionHandlerBase
    {
        public override FunctionTypeEnum FunctionType => FunctionTypeEnum.Report;

        public override FunctionHandlerResult Handle(FunctionHandlerContext context, Packet packet, ref Queue<Action<IMainHubEngine>> hubMessageQueue)
        {
            var queryableFunctionPairs = FunctionHandlerHelper.SplitPairs(packet.Text, context.ModuleConfiguration.FunctionResultDelimiter, context.ModuleConfiguration.FunctionResultNameDelimiter);
            var module = packet.Module;
            var moduleFunctions = context.DatabaseContext.ModuleFunctions
                                         .AsNoTracking()
                                         .Where(i => i.ModuleID == module.ID)
                                         .Select(i => i.FunctionID)
                                         .ToHashSet();

            foreach (var queryableFunctionPair in queryableFunctionPairs)
            {
                var functionName = queryableFunctionPair.Key;
                var functionID = context.FunctionNames.Forward.GetValueOrNullable(functionName);
                if (!functionID.HasValue)
                {
                    Log(context, packet,
                        MethodBase.GetCurrentMethod().GetFullName(),
                        $"Unrecognized function '{functionName}' received in the report.");

                    continue;
                }

                if (!moduleFunctions.Contains(functionID.Value))
                {
                    Log(context, packet,
                        MethodBase.GetCurrentMethod().GetFullName(),
                        $"Unbound function '{functionName}' received in the report.");

                    continue;
                }

                var functionType = context.FunctionTypes.Reverse[functionID.Value];
                var handler = context.QueryableFunctionHandlers.GetValueOrDefault(functionType);
                if (handler == null)
                {
                    Log(context, packet,
                        MethodBase.GetCurrentMethod().GetFullName(),
                        $"No handler found for function '{functionName}' received in the report.");

                    continue;
                }

                handler.Handle(context, packet, queryableFunctionPair.Value, hubMessageQueue);
            }

            return PacketStateEnum.Handled;
        }
    }
}