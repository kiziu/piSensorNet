using System;
using System.Linq;
using piSensorNet.Common;
using piSensorNet.Common.Custom;
using piSensorNet.Common.Extensions;
using piSensorNet.DataModel.Context;

namespace piSensorNet.Engine
{
    public static class EngineMainV2
    {
        private static IpiSensorNetConfiguration Configuration { get; } = ReadOnlyConfiguration.Load<IpiSensorNetConfiguration>("config.json");

        public static int Main(string[] args)
        {
            var result = HandleArguments(args);
            if (result.HasValue) return result.Value;

            return 0;
        }

        private static void Debug(string text)
            => Console.WriteLine($"=== {DateTime.Now.ToFullTimeString()}: {text}");

        private static int? HandleArguments(string[] args)
        {
            var recreate = args.Any(i => String.Equals(i, "recreate", StringComparison.InvariantCultureIgnoreCase));
            var recreateOnly = args.Any(i => String.Equals(i, "recreateOnly", StringComparison.InvariantCultureIgnoreCase));
            var validateModel = args.Any(i => String.Equals(i, "validateModel", StringComparison.InvariantCultureIgnoreCase));

            var recreateDatabase = (recreate || recreateOnly) && !validateModel;

            PiSensorNetDbContext.Initialize(Configuration.ConnectionString, recreateDatabase);

            //PiSensorNetDbContext.Logger = Console.Write;

            //if (validateModel)
            //{
            //    PiSensorNetDbContext.CheckCompatibility(Configuration.ConnectionString);

            //    Debug("HandleArguments: Model validation finished, exiting!");

            //    return 0;
            //}

            //if (recreateOnly)
            //{
            //    ToConsole("Main: Database recreated, exiting!");

            //    return 0;
            //}

            //if (!recreate && !standalone && args.Length > 0)
            //{
            //    ToConsole($"Main: ERROR: Wrong arguments given: '{args.Join(" ")}'.");

            //    return 1;
            //}

            return null;
        }
    }
}
