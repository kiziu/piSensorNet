using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNet.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using piSensorNet.Common.JsonConverters;
using piSensorNet.DataModel.Context;
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

        public void Configure(IApplicationBuilder applicationBuilder, JsonSerializer jsonSerializer)
        {
            applicationBuilder.UseIISPlatformHandler()
                               .UseStaticFiles()
                               .UseDeveloperExceptionPage()
                               .UseMvc(routes =>
                               {
                                   routes.MapRoute("area", "{area:exists:regex(^(?!Root).)}/{controller}/{action=Index}/{id?}");
                                   routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}",
                                       new
                                       {
                                           area = "Root"
                                       });

                                   routes.MapRoute("full", "{controller}/{action}",
                                       new
                                       {
                                           area = "Root"
                                       });

                                   routes.MapRoute("controllerOnly", "{controller}/{action=Index}",
                                       new
                                       {
                                           area = "Root"
                                       });
                               })
                               .UseSignalR();

            jsonSerializer.Converters.Add(new StringEnumConverter());
        }
    }
}