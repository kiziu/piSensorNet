using System;
using System.Collections.Generic;
using System.Linq;
using piSensorNet.Common;
using piSensorNet.DataModel.Context;
using piSensorNet.DataModel.Entities;
using piSensorNet.DataModel.Enums;
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

        protected override IReadOnlyCollection<string> GetItems(IModuleConfiguration moduleConfiguration, Packet packet)
            => FunctionHandlerHelper.SplitSingle(packet.Text, moduleConfiguration.FunctionResultDelimiter);

        protected override Func<string, string> GetAddress => item => item;

        protected override void OnHandled(PiSensorNetDbContext context, Module module, Queue<Action<IMainHubEngine>> hubMessageQueue, IReadOnlyCollection<TemperatureSensor> newSensors)
        {
            var dictionary = newSensors.ToDictionary(i => i.ID, i => i.Address);

            hubMessageQueue.Enqueue(proxy => proxy.NewOneWireDevices(module.ID, dictionary));
        }
    }
}
