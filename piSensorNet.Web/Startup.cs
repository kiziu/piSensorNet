using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using piSensorNet.Common;
using piSensorNet.Common.Custom;
using piSensorNet.Common.Enums;
using piSensorNet.Common.JsonConverters;
using piSensorNet.Common.System;
using piSensorNet.DataModel.Context;
using piSensorNet.DataModel.Entities;
using piSensorNet.DataModel.Extensions;
using piSensorNet.Web.Controllers;
using Module = piSensorNet.DataModel.Entities.Module;
using static piSensorNet.Common.Helpers.LoggingHelper;

namespace piSensorNet.Web
{
    public class Startup
    {
        public static IpiSensorNetConfiguration Configuration { get; } = ReadOnlyConfiguration.Load<IpiSensorNetConfiguration>("config.json");
        private static string ConnectionString => Configuration["Settings:ConnectionString"];

        public Startup()
        {
            ToConsole("Starting...");

            PiSensorNetDbContext.Initialize(ConnectionString);

            ToConsole("Context initialized!");

            var debugProfile = Environment.GetEnvironmentVariable("DEBUG_PROFILE");
            if (debugProfile?.Equals("vs", StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                var config = new ConfigurationBuilder().AddJsonFile("Properties/launchSettings.json").Build();
                var url = config[$"profiles:{debugProfile}:launchUrl"];

                Process.Start("chrome", url);

                //LoadDemoData(ConnectionString);
            }
        }

        [Obsolete]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private static void LoadDemoData(string connectionString)
        {
            using (var context = PiSensorNetDbContext.Connect(connectionString).WithAutoSave())
            {
                if (context.Modules.FirstOrDefault() != null)
                    return;

                var functions = context.Functions.AsNoTracking().ToDictionary(i => i.FunctionType, i => i.ID);

                var module1 = context.Modules.Add(new Module("1izik") {FriendlyName = "kiziu no 1", State = ModuleStateEnum.Identified});
                context.Modules.Add(new Module("2izik") {FriendlyName = "kiziu no 2"});
                context.Modules.Add(new Module("3izik") {FriendlyName = "kiziu no 3"});

                context.SaveChanges();

                context.ModuleFunctions.AddRange(functions.Select(i => new ModuleFunction(module1.ID, i.Value)));

                context.TemperatureSensors.Add(
                    new TemperatureSensor(module1.ID, "2854280E02000070"),
                    new TemperatureSensor(module1.ID, "28AC5F2600008030") {FriendlyName = "Sonda"});
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                    .AddJsonOptions(options => { options.SerializerSettings.Converters.Add(new ExtendedEnumConverter()); });

            services.AddTransient<Func<PiSensorNetDbContext>>(provider =>
                () => PiSensorNetDbContext.Connect(ConnectionString));

            services.AddSignalR(options => { options.Hubs.EnableDetailedErrors = true; });
        }

        public void Configure(IApplicationBuilder applicationBuilder, IApplicationLifetime applicationLifetime, JsonSerializer jsonSerializer)
        {
            //applicationLifetime.ApplicationStopped.Register(ApplicationStoppedHandler);

            applicationBuilder.UseIISPlatformHandler()
                              .UseSignalR()
                              .UseStaticFiles()
                              .UseDeveloperExceptionPage()
                              .UseMvc(routes => ConfigureRoutes<HomeController>(routes,
                                  Reflector.Instance<HomeController>.ControllerName,
                                  nameof(HomeController.Index)));

            jsonSerializer.Converters.Add(new StringEnumConverter());
        }
        
        private static void ConfigureRoutes<TMainController>(IRouteBuilder routes, string mainController, string mainAction)
        {
            var mianAreaName = Reflector.Instance<TMainController>.Type
                                        .GetCustomAttribute<AreaAttribute>()
                                       ?.RouteValue;

            if (string.IsNullOrEmpty(mianAreaName))
                throw new ArgumentException($"Type '{Reflector.Instance<TMainController>.Name}' is not properly marked with '{Reflector.Instance<AreaAttribute>.Name}'.");

            var defaults = new {area = mianAreaName};

            routes.MapRoute("full", "{controller}/{action}", defaults);
            routes.MapRoute("controllerOnly", $"{{controller}}/{{action={mainAction}}}", defaults);
            routes.MapRoute("default", $"{{controller={mainController}}}/{{action={mainAction}}}/{{id?}}", defaults);

            routes.MapRoute("area", $"{{area:exists:regex(^(?!{mianAreaName}).)}}/{{controller}}/{{action={mainAction}}}/{{id?}}");
        }
    }
}