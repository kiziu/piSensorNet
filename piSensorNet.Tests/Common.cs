using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using piSensorNet.Common;
using piSensorNet.Common.Configuration;
using piSensorNet.Common.Extensions;
using piSensorNet.Common.Helpers;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace piSensorNet.Tests
{
    internal interface ITestsConfiguration : IpiSensorNetConfiguration
    {
        /// <summary>
        /// 0 - address, 1 - number, 2 - current, 3 - total, 4 - text 
        /// </summary>
        string MessagePattern { get; }
    }

    internal static class Common
    {
        public static readonly IpiSensorNetConfiguration Configuration = ReadOnlyConfiguration.Load("config.json");
        public static readonly ITestsConfiguration TestsConfiguration = ReadOnlyConfiguration.Proxify<ITestsConfiguration>((IConfiguration)Configuration.GetType().GetField("Configuration").GetValue(Configuration));

        public static readonly Action<string> ConsoleLogger = LoggingHelper.ToConsole;
        public static readonly Action<string> EmptyLogger = i => { };
    }

    internal sealed class ConsoleHubProxy : IHubProxy
    {
        private const string Null = "<null>";

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
            return Task.Factory.StartNew(
                () => _echo("Hub => {0}({1})", method, args.Select(i => i?.ToString() ?? Null).Join(", ")));
        }

        public Task<T> Invoke<T>(string method, params object[] args)
        {
            return Task.Factory.StartNew(
                () =>
                {
                    _echo("Hub => {0}({1})", method, method, args.Select(i => i?.ToString() ?? Null).Join(", "));

                    return default(T);
                });
        }

        public Task Invoke<T>(string method, Action<T> onProgress, params object[] args)
        {
            return Task.Factory.StartNew(
                () => _echo("Hub => {0}({1}), with progress", method, args.Select(i => i?.ToString() ?? Null).Join(", ")));
        }

        public Task<TResult> Invoke<TResult, TProgress>(string method, Action<TProgress> onProgress, params object[] args)
        {
            return Task.Factory.StartNew(
                () =>
                {
                    _echo("Hub => {0}({1}), with progress", method, args.Select(i => i?.ToString() ?? Null).Join(", "));

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