using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using piSensorNet.Common.Enums;
using piSensorNet.Common.JsonConverters;
using piSensorNet.Common.System;
using piSensorNet.DataModel.Context;
using piSensorNet.DataModel.Entities;
using piSensorNet.DataModel.Extensions;
using piSensorNet.Web.Controllers;
using IConfiguration = piSensorNet.Common.IConfiguration;
using Module = piSensorNet.DataModel.Entities.Module;

namespace piSensorNet.Web
{
    public class Startup
    {
        private static string Now => DateTime.Now.ToString("O");

        public static IConfiguration Configuration { get; } = Common.Configuration.Load("config.json");
        private static string ConnectionString => Configuration["Settings:ConnectionString"];

        public Startup()
        {
            Console.WriteLine($"{Now}: Starting...");

            PiSensorNetDbContext.Initialize(ConnectionString);

            Console.WriteLine($"{Now}: Context initialized!");

            var debugProfile = Environment.GetEnvironmentVariable("DEBUG_PROFILE");
            if (debugProfile != null)
            {
                var config = new ConfigurationBuilder().AddJsonFile("Properties/launchSettings.json").Build();
                var url = config[$"profiles:{debugProfile}:launchUrl"];

                Process.Start("chrome", url);

                LoadDemoData(ConnectionString);
            }
        }

        private static void LoadDemoData(string connectionString)
        {
            using (var context = PiSensorNetDbContext.Connect(connectionString).WithAutoSave())
            {
                if (context.Modules.FirstOrDefault() != null)
                    return;

                var functions = context.Functions.AsNoTracking().ToDictionary(i => i.FunctionType, i => i.ID);

                var kizi1 = context.Modules.Add(new Module("1izik") {FriendlyName = "kiziu no 1", State = ModuleStateEnum.Identified});
                context.Modules.Add(new Module("2izik") {FriendlyName = "kiziu no 2"});
                context.Modules.Add(new Module("3izik") {FriendlyName = "kiziu no 3"});

                context.SaveChanges();

                context.ModuleFunctions.AddRange(functions.Select(i => new ModuleFunction(kizi1.ID, i.Value)));

                context.TemperatureSensors.Add(
                    new TemperatureSensor(kizi1.ID, "2854280E02000070"),
                    new TemperatureSensor(kizi1.ID, "28AC5F2600008030") {FriendlyName = "Sonda"});
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                    .AddJsonOptions(options =>
                                    {
                                        options.SerializerSettings.Converters.Add(new ExtendedEnumConverter());
                                    });

            services.AddTransient<Func<PiSensorNetDbContext>>(provider =>
                () => PiSensorNetDbContext.Connect(ConnectionString));

            services.AddSignalR(options =>
                                {
                                    options.Hubs.EnableDetailedErrors = true;
                                });
        }

        public void Configure(IApplicationBuilder applicationBuilder, JsonSerializer jsonSerializer)
        {
            applicationBuilder.UseIISPlatformHandler()
                              .UseSignalR()
                              .UseStaticFiles()
                              .UseDeveloperExceptionPage()
                              .UseMvc(routes => ConfigureRoutes<HomeController>(routes,
                                  Reflector.Instance<HomeController>.ControllerName,
                                  Reflector.Instance<HomeController>.Method<IActionResult>(i => i.Index).Name));

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