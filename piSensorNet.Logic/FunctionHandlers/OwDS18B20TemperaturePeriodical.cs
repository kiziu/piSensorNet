using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using piSensorNet.Common.Enums;
using piSensorNet.Common.Extensions;
using piSensorNet.DataModel.Entities;
using piSensorNet.Logic.Custom;
using piSensorNet.Logic.FunctionHandlers.Base;

namespace piSensorNet.Logic.FunctionHandlers
{
    internal sealed class OwDS18B20TemperaturePeriodical : FunctionHandlerBase, IQueryableFunctionHandler
    {
        public override FunctionTypeEnum FunctionType => FunctionTypeEnum.OwDS18B20TemperaturePeriodical;

        public override FunctionHandlerResult Handle(FunctionHandlerContext context, Packet packet, ref HubMessageQueue hubMessageQueue)
            => Handle(context, packet, packet.Text, hubMessageQueue);

        public PacketStateEnum Handle(FunctionHandlerContext context, Packet originalPacket, string response, HubMessageQueue hubMessageQueue)
        {
            var module = originalPacket.Module;

            int periodUnits;
            if (!int.TryParse(response, NumberStyles.Integer, CultureInfo.InvariantCulture, out periodUnits))
                return LogAndReturn(context, originalPacket,
                    MethodBase.GetCurrentMethod().GetFullName(),
                    $"Could not parse text '{originalPacket.Text}' to integer period units.");

            if (periodUnits < 0)
                return LogAndReturn(context, originalPacket,
                    MethodBase.GetCurrentMethod().GetFullName(),
                    $"Period units {periodUnits} cannot be negative.");

            var periodLengthinMs = periodUnits * context.ModuleConfiguration.PeriodUnitLengthInMs;
            var period = TimeSpan.FromMilliseconds(periodLengthinMs);

            context.DatabaseContext.EnqueueUpdate<TemperatureSensor>(
                i => i.Period == period,
                i => i.ModuleID == module.ID);

            context.DatabaseContext.ExecuteRaw();

            hubMessageQueue.Enqueue(i => i.ChangedTemperatureSensorPeriod(module.ID, period));

            return PacketStateEnum.Handled;
        }
    }
}