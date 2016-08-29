using System;
using System.Linq;
using piSensorNet.Common;
using piSensorNet.Common.Configuration;
using piSensorNet.Common.Enums;
using piSensorNet.DataModel.Context;
using piSensorNet.DataModel.Entities;
using piSensorNet.Engine;

namespace piSensorNet.EngineTester
{
    public class TesterMain
    {
        private static IpiSensorNetConfiguration Configuration { get; } = ReadOnlyConfiguration.Load();

        public static int Main(string[] args)
        {
            using (var context = PiSensorNetDbContext.Connect(Configuration.ConnectionString)
                                                     .WithChangeTracking()
                                                     .WithAutoSave())
            {
                //var module = context.Modules.Add(new Module("1izik")
                //{
                //    ModuleFunctions = context.Functions
                //                                                              .AsEnumerable()
                //                                                              .Select(i => new ModuleFunction(0, i.ID))
                //                                                              .ToList()
                //});

                var trigger = new Trigger("Test1", "", null)
                              {
                                  TriggerSources = new[]
                                                   {
                                                       new TriggerSource(0, TriggerSourceTypeEnum.AbsoluteTime)
                                                       {
                                                           AbsoluteTime = TimeSpan.Parse("12:13:21")
                                                       }
                                                   },
                                  TriggerDependencies = new[]
                                                        {
                                                            new TriggerDependency(0, TriggerDependencyTypeEnum.Communication),
                                                            new TriggerDependency(0, TriggerDependencyTypeEnum.LastTemperatureReadout)
                                                        }
                              };

                if (context.Triggers.Where(i => i.FriendlyName == trigger.FriendlyName).SingleOrDefault() == null)
                    context.Triggers.Add(trigger);
            }

            EngineMain.Main(new[] {"standalone"});

            return 0;
        }
    }
}