using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using piSensorNet.Common;
using piSensorNet.Common.Extensions;

namespace piSensorNet.Tests
{
    internal interface ITestsConfiguration : IModuleConfiguration
    {
        /// <summary>
        /// 0 - address, 1 - number, 2 - current, 3 - total, 4 - text 
        /// </summary>
        string MessagePattern { get; }
    }

    internal static class Common
    {
        public static readonly IConfiguration Configuration = piSensorNet.Common.Configuration.Load("config.json");
        public static readonly ITestsConfiguration TestsConfiguration = new _TestsConfiguration(Configuration);

        public static readonly Action<string> ConsoleLogger = i => Console.WriteLine($"{DateTime.Now.ToString("O")}: {i}");
        public static readonly Action<string> EmptyLogger = i => { };

        internal sealed class _TestsConfiguration : ModuleConfiguration, ITestsConfiguration
        {
            public _TestsConfiguration(IConfiguration configuration)
                : base(configuration)
            {
                MessagePattern = configuration["Settings:MessagePattern"];
            }

            public string MessagePattern { get; }
        }
    }

    internal sealed class ConsoleHubProxy : IHubProxy
    {
        private delegate void Echo(string pattern, params object[] args);

        private readonly Echo _echo;

        public ConsoleHubProxy(bool echo)
        {
            if (echo)
                _echo = Console.WriteLine;
            else
                _echo = (pattern, args) => { };
        }

        public Task Invoke(string method, params object[] args)
        {
            return Task.Factory.StartNew(() => _echo("Hub => {0}({1})", method, args.Select(i => i.ToString()).Join(", ")));
        }

        public Task<T> Invoke<T>(string method, params object[] args)
        {
            return Task.Factory.StartNew(() =>
            {
                _echo("Hub => {0}({1})", method, method, args.Select(i => i.ToString()).Join(", "));

                return default(T);
            });
        }

        public Task Invoke<T>(string method, Action<T> onProgress, params object[] args)
        {
            return Task.Factory.StartNew(() => _echo("Hub => {0}({1}), with progress", method, args.Select(i => i.ToString()).Join(", ")));
        }

        public Task<TResult> Invoke<TResult, TProgress>(string method, Action<TProgress> onProgress, params object[] args)
        {
            return Task.Factory.StartNew(() =>
            {
                _echo("Hub => {0}({1}), with progress", method, args.Select(i => i.ToString()).Join(", "));

                return default(TResult);
            });
        }

        public Subscription Subscribe(string eventName)
        {
            throw new NotImplementedException();
        }

        public JToken this[string name] { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }

        public JsonSerializer JsonSerializer { get { throw new NotImplementedException(); } }
    }
}