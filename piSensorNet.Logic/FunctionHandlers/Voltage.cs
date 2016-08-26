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
    internal sealed class Voltage : FunctionHandlerBase
    {
        public override FunctionTypeEnum FunctionType => FunctionTypeEnum.Voltage;
        public override TriggerSourceTypeEnum? TriggerSourceType =>  TriggerSourceTypeEnum.VoltageReadout;

        public override FunctionHandlerResult Handle(FunctionHandlerContext context, Packet packet, ref HubMessageQueue hubMessageQueue)
        {
            var module = packet.Module;

            decimal reading;
            if (!decimal.TryParse(packet.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out reading))
                return LogAndReturn(context, packet,
                    MethodBase.GetCurrentMethod().GetFullName(),
                    $"Could not parse text '{packet.Text}' to decimal voltage.");

            var voltageReading = new VoltageReadout(module.ID, reading, packet.Received);

            context.DatabaseContext.VoltageReadouts.Add(voltageReading);
            context.DatabaseContext.SaveChanges();

            hubMessageQueue.Enqueue(i => i.NewVoltageReading(voltageReading.ModuleID, voltageReading.Value, voltageReading.Created, voltageReading.Received));

            return PacketStateEnum.Handled;
        }
    }
}
