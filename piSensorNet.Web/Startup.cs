using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using piSensorNet.Common.JsonConverters;
using piSensorNet.Common.System;
using piSensorNet.DataModel.Context;
using piSensorNet.Web.Controllers;
using IConfiguration = piSensorNet.Common.IConfiguration;

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

        public void Configure(IApplicationBuilder applicationBuilder, JsonSerializer jsonSerializer, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole((s, level) => true);

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

            routes.MapRoute("full", $"{{controller}}/{{action}}", new {area = mianAreaName});
            routes.MapRoute("controllerOnly", $"{{controller}}/{{action={mainAction}}}", new {area = mianAreaName});
            routes.MapRoute("default", $"{{controller={mainController}}}/{{action={mainAction}}}/{{id?}}", new {area = mianAreaName});

            routes.MapRoute("area", $"{{area:exists:regex(^(?!{mianAreaName}).)}}/{{controller}}/{{action=Index}}/{{id?}}");
        }
    }
}