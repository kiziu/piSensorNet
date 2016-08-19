using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.Common;
using piSensorNet.Common.Enums;
using piSensorNet.DataModel.Entities;
using piSensorNet.Logic.FunctionHandlers.Base;

namespace piSensorNet.Logic.FunctionHandlers
{
    // TODO KZ: extend functionality once more OW device types present

    /// <summary>
    ///  for now, assume OW devices are tempreature sensors
    /// </summary>
    internal sealed class OwList : TemperatureSensorsFinderBase<string>
    {
        public override FunctionTypeEnum FunctionType => FunctionTypeEnum.OwList;
        public override TriggerSourceTypeEnum? TriggerSourceType => null;

        protected override IReadOnlyCollection<string> GetItems(IModuleConfiguration moduleConfiguration, Packet packet)
            => FunctionHandlerHelper.SplitSingle(packet.Text, moduleConfiguration.FunctionResultDelimiter);

        protected override Func<string, string> GetAddress => item => item;

        protected override void OnHandled(FunctionHandlerContext context, Module module, Queue<Action<IMainHubEngine>> hubMessageQueue, IReadOnlyCollection<TemperatureSensor> newSensors)
        {
            var dictionary = newSensors.ToDictionary(i => i.ID, i => i.Address);

            hubMessageQueue.Enqueue(proxy => proxy.NewOneWireDevices(module.ID, dictionary));
        }
    }
}
