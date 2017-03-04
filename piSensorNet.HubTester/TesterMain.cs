using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using piSensorNet.Common.Custom;
using piSensorNet.Common.Custom.Interfaces;
using piSensorNet.Common.Enums;
using piSensorNet.Common.Extensions;
using piSensorNet.Common.JsonConverters;

using static piSensorNet.Common.Helpers.LoggingHelper;

namespace piSensorNet.HubTester
{
    public delegate void UserFunctionDelegate(IReadOnlyDictionary<string, int> modules);

    public static class TesterMain
    {
        private static IReadOnlyConfiguration Configuration { get; } = ReadOnlyConfiguration.Load<IReadOnlyConfiguration>();
        
        public static int Main(string[] args)
        {
            DisposalQueue toDispose;
            var hubProxy = InitializeHubConnection(Configuration, InternalHandleMessage, out toDispose, ToConsole);

            ToConsole("Main: Started!");

            while (true)
            {
                var line = Console.ReadLine();

                if (String.IsNullOrWhiteSpace(line))
                    break;

                if (hubProxy == null)
                {
                    ToConsole("Not connected to hub...");
                    continue;
                }

                var parts = line.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);

                var method = parts[0];
                var arguments = parts.Skip(1).ToList();
                var typedArguments = new object[arguments.Count];

                for (var i = 0; i < arguments.Count; ++i)
                {
                    var item = arguments[i].Split(':');
                    object typedArgument;

                    if (item.Length == 1)
                        typedArgument = item[0];
                    else
                        switch (item[0])
                        {
                            case "s":
                                typedArgument = item[1].TrimToNull();
                                break;

                            case "i":
                                typedArgument = int.Parse(item[1]);
                                break;

                            case "d":
                                typedArgument = decimal.Parse(item[1]);
                                break;

                            case "dt":
                                typedArgument = DateTime.Parse(item[1]);
                                break;

                            case "ts":
                                typedArgument = TimeSpan.Parse(item[1]);
                                break;

                            case "li,f":
                                typedArgument = JsonConvert.DeserializeObject<List<FunctionTypeEnum>>(item[1]);
                                break;

                            case "di,i,s":
                                typedArgument = JsonConvert.DeserializeObject<Dictionary<int, string>>(item[1]);
                                break;

                            default:
                                ToConsole($"Error: Type '{item[0]}' is not recognized!");
                                goto NEXT;
                        }

                    typedArguments[i] = typedArgument;
                }

                ToConsole($"=> {method}({typedArguments.Select(i => i?.ToString() ?? "<null>").Join(", ")})");
                hubProxy.SafeInvoke(method, typedArguments);

            NEXT:
                ;
            }
            

            toDispose?.Dispose();

            ToConsole("Main: Stopped!");

            return 0;
        }
        
        private static IHubProxy InitializeHubConnection(IReadOnlyConfiguration configuration, Action<string, int?, FunctionTypeEnum, bool, string, Action<string>> handler, out DisposalQueue toDispose, Action<string> logger)
        {
            toDispose = new DisposalQueue();

            var hubConnection = new HubConnection(configuration["Settings:WebAddress"],
                new Dictionary<string, string>
                {
                    {
                        configuration["Settings:SignalREngineFlagName"], true.ToString().ToLowerInvariant()
                    }
                });

            hubConnection.StateChanged += change => logger($"InitializeHubConnection: StateChanged: '{change.OldState}' -> '{change.NewState}'!");

            hubConnection.JsonSerializer.Converters.Add(new NullConverter()); // handle NULLs

            var hubProxy = hubConnection.CreateHubProxy(configuration["Settings:SignalRHubName"]);

            toDispose.Enqueue(hubProxy.On<string, int?, FunctionTypeEnum, string>("sendMessage",
                (clientID, moduleID, functionType, text)
                    => handler(clientID, moduleID, functionType, false, text, logger)));

            toDispose.Enqueue(hubProxy.On<string, int?, FunctionTypeEnum>("sendQuery",
                (clientID, moduleID, functionType)
                    => handler(clientID, moduleID, functionType, true, null, logger)));

            try
            {
                hubConnection.Start().Wait();
            }
            catch (Exception e)
            {
                var message = $"InitializeHubConnection: ERROR:{Environment.NewLine}Exception '{typeof(Exception).Name}' occurred while initializing hub connection: {e.Message}.";
                if (e.InnerException != null)
                    message += Environment.NewLine
                               + $"Inner exception '{e.InnerException.GetType().Name}': {e.InnerException.Message}.";

                logger(message);

                toDispose = null;
                return null;
            }

            logger($"InitializeHubConnection: Connection to hub started with ID '{hubConnection.ConnectionId}'!");

            toDispose.Enqueue(hubConnection);

            return hubProxy;
        }
        
        private static void InternalHandleMessage(string clientID, int? moduleID, FunctionTypeEnum functionType, bool isQuery, string text, Action<string> logger)
        {
            var message = $"<= @{moduleID?.ToString() ?? "eizik"} {functionType}{(isQuery ? "?" : (text != null ? ":" + text : String.Empty))}";

            logger(message);
        }
    }
}
