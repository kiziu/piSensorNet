using System;
using System.Linq;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using piSensorNet.Common;
using piSensorNet.DataModel.Context;
using piSensorNet.Logic.Services;

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
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddTransient<Func<PiSensorNetDbContext>>(provider =>
                () => PiSensorNetDbContext.Connect(ConnectionString));

            services.AddSingleton<ModulesService>();

            services.AddSignalR(options =>
            {
                options.Hubs.EnableDetailedErrors = true;
            });
        }

        public void Configure(IApplicationBuilder applicationBuilder, IApplicationLifetime applicationLifetime, IConnectionManager connectionManager)
        {
            applicationBuilder.UseIISPlatformHandler();

            applicationBuilder.UseStaticFiles();
            
            applicationBuilder.UseDeveloperExceptionPage();

            applicationBuilder.UseMvcWithDefaultRoute();
            
            applicationBuilder.UseSignalR();
        }
    }
}