using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.AspNet.SignalR.Client;
using piSensorNet.Common;
using piSensorNet.Common.Custom;
using piSensorNet.Common.Extensions;
using piSensorNet.DataModel.Context;
using piSensorNet.DataModel.Entities;
using piSensorNet.DataModel.Enums;
using piSensorNet.DataModel.Extensions;
using piSensorNet.Common.System;
using piSensorNet.Engine.SignalR;
using piSensorNet.Logic;
using piSensorNet.Logic.FunctionHandlers.Base;
using Module = piSensorNet.DataModel.Entities.Module;

[assembly: InternalsVisibleTo("piSensorNet.Tests")]

namespace piSensorNet.Engine
{
    public delegate void UserFunctionDelegate(IReadOnlyDictionary<string, int> modules);

    public static class EngineMain
    {
        private static readonly EventWaitHandle WaitHandle = new AutoResetEvent(false);

        private static readonly IReadOnlyDictionary<SignalTypeEnum, Action<SignalTypeEnum>> SignalHandlers =
            new Dictionary<SignalTypeEnum, Action<SignalTypeEnum>>
            {
                {SignalTypeEnum.Quit, QuitSignalHandler},
                {SignalTypeEnum.Interrupt, QuitSignalHandler},
                {SignalTypeEnum.User1, NewPartialPacketsToMergeSignalHandler},
                {SignalTypeEnum.HangUp, RedoCacheSignalHandler},
            };


        private static IConfiguration Configuration { get; } = Common.Configuration.Load("config.json");
        private static IModuleConfiguration ModuleConfiguration { get; } = new ModuleConfiguration(Configuration);

        private static volatile bool _doQuit;
        private static volatile bool _pollPartialPackets;
        private static volatile bool _pollPackets;

        private static readonly Action<string> Logger = i => Console.WriteLine($"{DateTime.Now.ToString("O")}: {i}");

        private static IReadOnlyDictionary<string, int> Modules;
        private static IReadOnlyDictionary<int, string> InverseModules;
        private static IReadOnlyDictionary<int, IFunctionHandler> FunctionHandlers;
        private static IReadOnlyDictionary<string, IQueryableFunctionHandler> QueryableFunctionHandlers;
        private static IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> Functions;
        private static IReadOnlyDictionary<string, int> InverseFunctions;

        public static int Main(string[] args)
        {
            Logger("Main: Initializing Engine...");

            var recreate = args.Any(i => String.Equals(i, "recreate", StringComparison.InvariantCultureIgnoreCase));
            var recreateOnly = args.Any(i => String.Equals(i, "recreateOnly", StringComparison.InvariantCultureIgnoreCase));
            var standalone = args.Any(i => String.Equals(i, "standalone", StringComparison.InvariantCultureIgnoreCase));

            int? serialProcessID = null;
            if (!standalone)
                serialProcessID = FindSerialProcessID(Configuration, recreate || recreateOnly, Logger);
            
            /*
            Logger("Main: Compiling...");

            var methodBody = "Console.WriteLine(\"works! \" + modules[\"kizior\"]);";
            var result = CompileHelper.CompileTo<UserFunctionDelegate>(methodBody);
            
            if (!result.IsSuccessful)
            {
                Logger("Errors:");
                result.CompilerErrors.Each(e => Console.WriteLine($"({e.Line}, {e.Column}) [{e.ErrorNumber}] {e.ErrorText}"));

                return -1;
            }

            Logger("Main: Done!");
            Console.WriteLine(result.Body);
            result.Method(new Dictionary<string, int>
                                    {
                                        {"kizior", 13}
                                    });

            return -1;
            */

            PiSensorNetDbContext.Initialize(ModuleConfiguration.ConnectionString, recreate || recreateOnly);

            //PiSensorNetDbContext.Logger = Console.Write;

            if (recreateOnly)
            {
                Logger("Main: Database recreated, exiting!");

                return 0;
            }

            if (!recreate && !standalone && args.Length > 0)
            {
                Logger($"Main: ERROR: Wrong arguments given: '{args.Join(" ")}'.");

                return 1;
            }

            Logger("Main: Context initialized!");

            BuildCache();

            //Demo();
            //return 666;

            DisposalQueue toDispose;
            var hubProxy = InitializeHubConnection(Configuration, ModuleConfiguration, InternalHandleMessage, InverseModules, Functions, serialProcessID, out toDispose, Logger);

            var handler = Signal.Handle(SignalHandlers);

            Logger("Main: Started!");

            while (!_doQuit)
            {
                if (Constants.IsWindows)
                    WaitHandle.WaitOne(500);
                else
                    WaitHandle.WaitOne();

                if (_pollPartialPackets || Constants.IsWindows)
                {
                    _pollPartialPackets = false;

                    using (var context = PiSensorNetDbContext.Connect(ModuleConfiguration.ConnectionString))
                    {
                        _pollPackets = MergePackets(context, ModuleConfiguration, Functions, InverseFunctions, Modules, InternalCacheModules, Logger);

                        if (_pollPackets)
                        {
                            _pollPackets = false;

                            while (HandlePackets(context, ModuleConfiguration, Functions, FunctionHandlers, QueryableFunctionHandlers, hubProxy, serialProcessID, Logger)) { }
                        }
                    }
                }
            }

            Logger("Main: Stopping...");

            toDispose?.Dispose();
            handler.Dispose();

            Logger("Main: Stopped!");

            return 0;
        }

        private static void BuildCache()
        {
            using (var context = PiSensorNetDbContext.Connect(ModuleConfiguration.ConnectionString))
            {
                InternalCacheModules(context);

                var cachedFunctions = CacheFunctions(context);
                Functions = cachedFunctions.Item1;
                InverseFunctions = cachedFunctions.Item2;

                var cachedFunctionHandlers = CacheFunctionHandlers(Functions);
                FunctionHandlers = cachedFunctionHandlers.Item1;
                QueryableFunctionHandlers = cachedFunctionHandlers.Item2;
            }

            Logger("BuildCache: Cache built!");
        }

        private static Tuple<IReadOnlyDictionary<string, int>, IReadOnlyDictionary<int, string>> InternalCacheModules(PiSensorNetDbContext context)
        {
            var cachedModules = CacheModules(context);

            Modules = cachedModules.Item1;
            InverseModules = cachedModules.Item2;

            return cachedModules;
        }

        // ReSharper disable once UnusedMember.Local
        private static void Demo()
        {
            using (var context = PiSensorNetDbContext.Connect(ModuleConfiguration.ConnectionString).WithChangeTracking())
            {
                var functionID = context.Functions.AsNoTracking().Where(i => i.FunctionType == FunctionTypeEnum.FunctionList).Select(i => i.ID).Single();
                var moduleNumber = context.Modules.AsNoTracking().Count() + 1;
                var address = $"test{moduleNumber}";

                context.Modules
                       .Add(new Module(address)
                       {
                           FriendlyName = address,
                           Description = "Test module",
                       }
                           .Modify(i => i.ModuleFunctions.Add(context.Functions.Select(f => new ModuleFunction(i, f))))
                           .Modify(i => i.Packets.Add(new[]
                                                      {
                                                          new Packet(i, 0, "identify;function_list;voltage;report;ow_list;ow_ds18b20_temperature;ow_ds18b20_temperature_periodical;", DateTime.Now) // "2854280E02000070|25.75;28AC5F2600008030|25.75;"
                                                          {
                                                              FunctionID = functionID,
                                                          }
                                                      }))
                    );

                context.SaveChanges();

                var module = context.Modules.AsNoTracking().Where(i => i.Address == address).Single();
                var packet = context.Packets.AsNoTracking().Where(i => i.ModuleID == module.ID).OrderByDescending(i => i.Created).First();

                // ReSharper disable once PossibleInvalidOperationException
                var functionHandler = FunctionHandlers[packet.FunctionID.Value];
                var taskQueue = new Queue<Action<IMainHubEngine>>();

                functionHandler.Handle(ModuleConfiguration, context, packet, QueryableFunctionHandlers, Functions, ref taskQueue);

                context.SaveChanges();
            }
        }

        #region Init

        private static int? FindSerialProcessID(IConfiguration configuration, bool canContinue, Action<string> logger)
        {
            if (Constants.IsWindows)
                if (!canContinue)
                    throw new Exception("Linux only!");
                else
                    return null;

            var nameFragment = configuration["Settings:SerialProcessNameFragment"];

            var pid = Processess.FindByFragment(nameFragment, StringComparison.InvariantCulture, "sudo");
            if (!pid.HasValue)
            {
                logger($"FindSerialProcessID: ERROR: Serial process with name fragment '{nameFragment}' not found!");
                Environment.Exit(-1);
            }

            logger($"FindSerialProcessID: Found process #{pid.Value} with name fragment '{nameFragment}!");

            return pid.Value;
        }

        private static IMainHubEngine InitializeHubConnection(IConfiguration configuration, IModuleConfiguration moduleConfiguration, Action<string, int?, FunctionTypeEnum, bool, string, IReadOnlyDictionary<int, string>, IModuleConfiguration, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>>, int?, IHubProxy, Action<string>> handler, IReadOnlyDictionary<int, string> inverseModules, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> functions, int? serialProcessID, out DisposalQueue toDispose, Action<string> logger)
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

            var hubProxy = hubConnection.CreateHubProxy(configuration["Settings:SignalRHubName"]);

            toDispose.Enqueue(hubProxy.On<string, int?, FunctionTypeEnum, string>("sendMessage",
                (clientID, moduleID, functionType, text)
                    => handler(clientID, moduleID, functionType, false, text, inverseModules, moduleConfiguration, functions, serialProcessID, hubProxy, logger)));

            toDispose.Enqueue(hubProxy.On<string, int?, FunctionTypeEnum>("sendQuery",
                (clientID, moduleID, functionType)
                    => handler(clientID, moduleID, functionType, true, null, inverseModules, moduleConfiguration, functions, serialProcessID, hubProxy, logger)));

            try
            {
                hubConnection.Start().Wait();
            }
            catch (Exception e)
            {
                logger($"InitializeHubConnection: ERROR: Exception occurred while initializing hub connection: {e.Message}.");

                toDispose = null;
                return null;
            }

            logger($"InitializeHubConnection: Connection to hub started with ID '{hubConnection.ConnectionId}'!");

            toDispose.Enqueue(hubConnection);

            return new MainHubEngineProxy(hubProxy);
        }

        #endregion

        #region Caching

        internal static Tuple<IReadOnlyDictionary<int, IFunctionHandler>, IReadOnlyDictionary<string, IQueryableFunctionHandler>> CacheFunctionHandlers(IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> functions)
        {
            var baseType = typeof(IFunctionHandler);
            var handlerTypes = baseType.Assembly
                                       .GetTypes()
                                       .Where(i => i.IsNotPublic && i.IsSealed && i.IsClass)
                                       .Where(baseType.IsAssignableFrom)
                                       .ToList();

            var functionHandlers = new Dictionary<int, IFunctionHandler>();
            var queryableFunctionHandlers = new Dictionary<string, IQueryableFunctionHandler>();

            foreach (var handlerType in handlerTypes)
            {
                var instance = (IFunctionHandler)Activator.CreateInstance(handlerType);
                var function = functions.GetValueOrNullable(instance.FunctionType);

                if (!function.HasValue)
                    continue;

                functionHandlers.Add(function.Value.Key, instance);

                var queryableInstance = instance as IQueryableFunctionHandler;
                if (queryableInstance != null)
                    queryableFunctionHandlers.Add(function.Value.Value, queryableInstance);
            }

            return new Tuple<IReadOnlyDictionary<int, IFunctionHandler>, IReadOnlyDictionary<string, IQueryableFunctionHandler>>(functionHandlers, queryableFunctionHandlers);
        }

        internal static Tuple<IReadOnlyDictionary<string, int>, IReadOnlyDictionary<int, string>> CacheModules(PiSensorNetDbContext context)
        {
            var modules = context.Modules
                                 .AsNoTracking()
                                 .Select(i => new
                                 {
                                     i.ID,
                                     i.Address
                                 })
                                 .ToDictionary(i => i.Address, i => i.ID);

            var inverseModules = modules.ToDictionary(i => i.Value, i => i.Key);

            return new Tuple<IReadOnlyDictionary<string, int>, IReadOnlyDictionary<int, string>>(modules, inverseModules);
        }

        internal static Tuple<IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>>, IReadOnlyDictionary<string, int>> CacheFunctions(PiSensorNetDbContext context)
        {
            var functions = context.Functions
                                   .AsNoTracking()
                                   .Select(i => new
                                   {
                                       i.FunctionType,
                                       i.ID,
                                       i.Name,
                                   })
                                   .ToDictionary(i => i.FunctionType, i => KeyValuePair.Create(i.ID, i.Name));

            var inverseFunctions = functions.ToDictionary(i => i.Value.Value, i => i.Value.Key);

            return new Tuple<IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>>, IReadOnlyDictionary<string, int>>(functions, inverseFunctions);
        }

        #endregion

        internal static bool MergePackets(PiSensorNetDbContext context, IModuleConfiguration moduleConfiguration, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> functions, IReadOnlyDictionary<string, int> inverseFunctions, IReadOnlyDictionary<string, int> modules, Func<PiSensorNetDbContext, Tuple<IReadOnlyDictionary<string, int>, IReadOnlyDictionary<int, string>>> cacheModulesFunction, Action<string> logger)
        {
            logger("MergePackets: Start...");

            var partialPackets = context.PartialPackets
                                        .AsNoTracking()
                                        .Where(i => i.State == PartialPacketStateEnum.New)
                                        .ToList();

            //logger("MergePackets: Done selecting..."); // ~14ms

            var groupedPackets = partialPackets.GroupBy(i => new
            {
                i.Address,
                i.Number,
                i.Total
            })
                                               .ToList();

            var newModules = new Dictionary<string, Module>();

            foreach (var packetGroup in groupedPackets)
            {
                var address = packetGroup.Key.Address;

                if (modules.ContainsKey(address) || newModules.ContainsKey(address))
                    continue;

                var module = new Module(address);

                context.Modules.Add(module);

                newModules.Add(address, module);
            }

            if (newModules.Count > 0)
            {
                logger("MergePackets: Done finding modules...");

                context.SaveChanges();

                logger("MergePackets: Done saving modules...");

                modules = cacheModulesFunction(context).Item1;
            }

            logger($"MergePackets: Done creating new modules, found {newModules.Count}!"); // 700us, no modules

            var packetGroupWithPacket = new List<Tuple<Packet, IEnumerable<PartialPacket>>>(groupedPackets.Count);
            foreach (var packetGroup in groupedPackets)
            {
                if (packetGroup.Count() != packetGroup.Key.Total)
                {
                    context.EnqueueRaw(PartialPacket.GenerateUpdate(context,
                        new Dictionary<Expression<Func<PartialPacket, object>>, string>
                        {
                            {i => i.State, PartialPacketStateEnum.Fragmented.ToSql()}
                        },
                        new Tuple<Expression<Func<PartialPacket, object>>, string, string>(i => i.ID, "IN", String.Concat("(", packetGroup.Select(i => i.ID.ToString()).Join(", "), ")"))));

                    continue;
                }

                var moduleID = modules[packetGroup.Key.Address];
                int? messageID = null;
                var text = packetGroup.OrderBy(i => i.Current).Select(i => i.Message).Concat();
                var received = packetGroup.Max(i => i.Received);

                var messageIDDelimiterIndex = text.IndexOf(moduleConfiguration.MessageIDDelimiter);
                if (messageIDDelimiterIndex > 0)
                {
                    messageID = text.Substring(0, messageIDDelimiterIndex).FromBase36();
                    text = text.Substring(messageIDDelimiterIndex + 1);
                }

                var functionName = text.SubstringBefore(moduleConfiguration.FunctionResultNameDelimiter).ToLowerInvariant();
                var functionID = inverseFunctions.GetValueOrNullable(functionName);

                if (functionID.HasValue)
                    text = text.Substring(functionName.Length + 1);

                var packet = new Packet(moduleID, packetGroup.Key.Number, text, received)
                {
                    MessageID = messageID,
                    FunctionID = functionID,
                };

                context.Packets.Add(packet);

                packetGroupWithPacket.Add(Tuple.Create(packet, (IEnumerable<PartialPacket>)packetGroup));
            }

            if (packetGroupWithPacket.Count > 0)
            {
                //logger("MergePackets: Done parsing packet groups!"); // ~3ms

                context.SaveChanges();

                logger("MergePackets: Saved changes!"); // ~23ms

                packetGroupWithPacket.Each(p =>
                    context.EnqueueRaw(PartialPacket.GenerateUpdate(context,
                        new Dictionary<Expression<Func<PartialPacket, object>>, string>
                        {
                            {i => i.PacketID, p.Item1.ID.ToSql()},
                            {i => i.State, PartialPacketStateEnum.Merged.ToSql()},
                        },
                        new Tuple<Expression<Func<PartialPacket, object>>, string, string>(i => i.ID, "IN", String.Concat("(", p.Item2.Select(i => i.ID.ToString()).Join(", "), ")")))));

                context.ExecuteRaw();

                logger("MergePackets: Updated partial packets!"); // ~15ms, 1 packet
            }

            logger($"MergePackets: Done, created {packetGroupWithPacket.Count} packet(s)!");

            return packetGroupWithPacket.Count > 0;
        }

        internal static bool HandlePackets(PiSensorNetDbContext context, IModuleConfiguration moduleConfiguration, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> functions, IReadOnlyDictionary<int, IFunctionHandler> functionHandlers, IReadOnlyDictionary<string, IQueryableFunctionHandler> queryableFunctionHandlers, IMainHubEngine hubProxy, int? serialProcessID, Action<string> logger)
        {
            logger("HandlePackets: Start...");

            var packets = context.Packets
                                 .Include(i => i.Module)
                                 .Include(i => i.Function)
                                 .Where(i => i.State == PacketStateEnum.New)
                                 .Where(i => i.FunctionID.HasValue)
                                 .OrderBy(i => i.Received)
                                 .ToList();

            logger("HandlePackets: Done selecting..."); // ~21ms

            if (packets.Count == 0)
                return false;

            var handleAgain = false;
            var newMesagesAdded = false;
            var hubTasksQueue = new Queue<Action<IMainHubEngine>>();
            foreach (var packet in packets)
            {
                // ReSharper disable once PossibleInvalidOperationException
                var handler = functionHandlers.GetValueOrDefault(packet.FunctionID.Value);
                if (handler == null)
                {
                    context.EnqueueRaw(Packet.GenerateUpdate(context,
                        new Dictionary<Expression<Func<Packet, object>>, string>
                        {
                            {i => i.State, PacketStateEnum.Unhandled.ToSql()}
                        },
                        new Tuple<Expression<Func<Packet, object>>, string, string>(i => i.ID, "=", packet.ID.ToSql())));

                    logger($"HandlePackets: Packet #{packet.ID} (function '{packet.Function.FunctionType}') could not be handled, no handler found!");

                    continue;
                }

                var functionHandlerResult = handler.Handle(moduleConfiguration, context, packet, queryableFunctionHandlers, functions, ref hubTasksQueue);

                handleAgain = handleAgain || functionHandlerResult.ShouldHandlePacketsAgain;
                newMesagesAdded = newMesagesAdded || functionHandlerResult.NewMessagesAdded;

                context.EnqueueRaw(Packet.GenerateUpdate(context,
                    new Dictionary<Expression<Func<Packet, object>>, string>
                    {
                        {i => i.State, functionHandlerResult.PacketState.ToSql()},
                        {i => i.Processed, DateTime.Now.ToSql()}
                    },
                    new Tuple<Expression<Func<Packet, object>>, string, string>(i => i.ID, "=", packet.ID.ToSql())));

                logger($"HandlePackets: Packet #{packet.ID} processed to '{functionHandlerResult.PacketState}'" +
                       $"{(functionHandlerResult.NewMessagesAdded ? ", new messaged were added" : String.Empty)}" +
                       $"{(functionHandlerResult.ShouldHandlePacketsAgain ? ", requested another handling" : String.Empty)}" +
                       "!");
            }

            logger($"HandlePackets: Done handling packets, processed {packets.Count}!"); // ~51ms

            context.ExecuteRaw();

            logger("HandlePackets: Queries executed!"); // ~13ms

            if (newMesagesAdded && serialProcessID.HasValue)
                Signal.Send(serialProcessID.Value, SignalTypeEnum.User1);

            while (hubTasksQueue.Count > 0)
                hubTasksQueue.Dequeue()(hubProxy);

            logger($"HandlePackets: Hub message(s) sent{(newMesagesAdded ? ", Serial signaled about new message(s)" : String.Empty)}{(handleAgain ? ", packet(s) will be handled again" : String.Empty)}!"); // ~10ms

            return handleAgain;
        }

        internal static void HandleMessage(string clientID, int? moduleID, FunctionTypeEnum functionType, bool isQuery, string text, IReadOnlyDictionary<int, string> inverseModules, IModuleConfiguration moduleConfiguration, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> functions, IHubProxy hubProxy, Action<string> logger)
        {
            logger($"HandleMessage: Received message from ${clientID} to @{moduleID?.ToString() ?? "ALL"} - {functionType}{(text == null ? (isQuery ? "?" : String.Empty) : ":" + text)}");

            if (moduleID.HasValue && !inverseModules.ContainsKey(moduleID.Value))
            {
                hubProxy.Invoke("error", clientID, $"Module #{moduleID.Value} does not exist.");

                logger($"HandleMessage: ERROR: Message not handled, module #{moduleID.Value} does not exist!");

                return;
            }

            int messageID;
            using (var context = PiSensorNetDbContext.Connect(moduleConfiguration.ConnectionString))
            {
                var message = new Message(functions[functionType].Key, isQuery)
                {
                    ModuleID = moduleID,
                };

                if (!isQuery)
                    message.Text = text;

                context.Messages.Add(message);

                context.SaveChanges();

                messageID = message.ID;
            }

            logger($"HandleMessage: Message handled to #{messageID}!"); // ~38ms
        }

        #region Signal Handlers

        private static void InternalHandleMessage(string clientID, int? moduleID, FunctionTypeEnum functionType, bool isQuery, string text, IReadOnlyDictionary<int, string> inverseModules, IModuleConfiguration moduleConfiguration, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> functions, int? serialProcessID, IHubProxy hubProxy, Action<string> logger)
        {
            HandleMessage(clientID, moduleID, functionType, isQuery, text, inverseModules, moduleConfiguration, functions, hubProxy, logger);

            if (serialProcessID.HasValue)
                Signal.Send(serialProcessID.Value, SignalTypeEnum.User1);
        }

        private static void QuitSignalHandler(SignalTypeEnum signalType)
        {
            Logger($"Received quit signal as '{signalType}'!");

            _doQuit = true;
            WaitHandle.Set();
        }

        private static void NewPartialPacketsToMergeSignalHandler(SignalTypeEnum signalType)
        {
            Logger("Received signal to process new message!");

            _pollPartialPackets = true;
            WaitHandle.Set();
        }

        private static void RedoCacheSignalHandler(SignalTypeEnum signalType)
        {
            Logger("Received signal to redo cache!");

            BuildCache();
        }

        #endregion
    }
}
