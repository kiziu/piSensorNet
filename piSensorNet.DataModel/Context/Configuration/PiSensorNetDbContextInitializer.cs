using System;
using System.Data.Entity;
using System.Linq;
using piSensorNet.Common.Enums;
using piSensorNet.DataModel.Entities;

namespace piSensorNet.DataModel.Context
{
    internal class PiSensorNetDbContextInitializer : DropCreateDatabaseAlways<PiSensorNetDbContext>
    {
        protected override void Seed(PiSensorNetDbContext context)
        {
            context.Functions
                   .AddRange(new[]
                             {
                                 new Function("identify", FunctionTypeEnum.Identify, false)
                                 {
                                     Description = "Response will contain full address.",
                                 },

                                 new Function("function_list", FunctionTypeEnum.FunctionList, false)
                                 {
                                     Description = "Response will contain list of supported functions.",
                                 },

                                 new Function("voltage", FunctionTypeEnum.Voltage, false)
                                 {
                                     Description = "Response will contain current voltage in V."
                                 },

                                 new Function("report", FunctionTypeEnum.Report, false)
                                 {
                                     Description = "Response will contain dictionary of supported queryable functions " +
                                                   "with respective query response."
                                 },

                                 new Function("ow_list", FunctionTypeEnum.OwList, false)
                                 {
                                     Description = "Response will contain list of connected OneWire devices."
                                 },

                                 new Function("ow_ds18b20_temperature", FunctionTypeEnum.OwDS18B20Temperature, false)
                                 {
                                     Description = "Response will contain dictionary of temperature values in °C " +
                                                   "per every DS18B20 device."
                                 },

                                 new Function("ow_ds18b20_temperature_periodical", FunctionTypeEnum.OwDS18B20TemperaturePeriodical, true)
                                 {
                                     Description = "Request will set the period in 100ms resolution (up to " +
                                                   "65535 units, 6553.5 minutes) for temperature reporting. Response " +
                                                   "will contain currently set period."
                                 }
                             });

            context.SaveChanges();
        }
    }
}
