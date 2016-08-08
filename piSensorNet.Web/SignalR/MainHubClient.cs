using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using piSensorNet.Common.Extensions;
using piSensorNet.DataModel.Enums;

namespace piSensorNet.Web.SignalR
{
    public partial class MainHub
    {
        public void Identify()
        {
            Console.WriteLine($"{Now}: {nameof(Identify)} ${Context.ConnectionId}");

            Engine.SendMessage(null, FunctionTypeEnum.Identify);
        }

        public void ReadTemperature(int? moduleID)
        {
            Console.WriteLine($"{Now}: {nameof(ReadTemperature)}({moduleID?.ToString() ?? "<null>"}) ${Context.ConnectionId}");

            Engine.SendMessage(moduleID, FunctionTypeEnum.OwDS18B20Temperature);
        }

        public void SetTemperatureReportPeriod(int moduleID, TimeSpan period)
        {
            Console.WriteLine($"{Now}: {nameof(SetTemperatureReportPeriod)}({moduleID}, {period}) ${Context.ConnectionId}");

            var value = (int)(period.TotalMilliseconds / 100);

            if (value < 0 || value > UInt16.MaxValue)
            {
                SendError($"Period '{period}' chosen for module #{moduleID} is out of valid range.");

                return;
            }

            var text = value.ToString("D", CultureInfo.InvariantCulture);

            Engine.SendMessage(moduleID, FunctionTypeEnum.OwDS18B20TemperaturePeriodical, text);
        }

        public async Task<IReadOnlyDictionary<int, object>> ListModules()
        {
            Console.WriteLine($"{Now}: {nameof(ListModules)} ${Context.ConnectionId}");

            return await Task.Factory.StartNew(() =>
            {
                using (var context = _contextFactory())
                {
                    return context.Modules
                                  .ToDictionary(i => i.ID,
                                      i => (object)new
                                                   {
                                                       i.ID,
                                                       i.FriendlyName,
                                                       i.Address,
                                                       i.State
                                                   });
                }
            });
        }

        public async Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, object>>> ListTemperatureSensors()
        {
            Console.WriteLine($"{Now}: {nameof(ListTemperatureSensors)} ${Context.ConnectionId}");

            return await Task.Factory.StartNew(() =>
            {
                using (var context = _contextFactory())
                {
                    return context.TemperatureSensors
                                  .AsEnumerable()
                                  .GroupBy(i => i.ModuleID)
                                  .ToDictionary(i => i.Key,
                                      i => i.ToDictionary(ii => ii.ID,
                                          ii => (object)new
                                                        {
                                                            ii.ID,
                                                            ii.Address,
                                                            ii.FriendlyName,
                                                            ii.Period,
                                                        })
                                            .ReadOnly());
                }
            });
        }
    }
}