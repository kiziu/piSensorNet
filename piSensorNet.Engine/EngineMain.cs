using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Timers;
using Microsoft.AspNet.SignalR.Client;
using piSensorNet.Common;
using piSensorNet.Common.Configuration;
using piSensorNet.Common.Custom;
using piSensorNet.Common.Custom.Interfaces;
using piSensorNet.Common.Enums;
using piSensorNet.Common.Extensions;
using piSensorNet.Common.JsonConverters;
using piSensorNet.DataModel.Context;
using piSensorNet.DataModel.Entities;
using piSensorNet.DataModel.Enums;
using piSensorNet.Common.System;
using piSensorNet.Logic.Compilation;
using piSensorNet.Logic.Custom;
using piSensorNet.Logic.FunctionHandlers.Base;
using piSensorNet.Logic.TriggerDependencyHandlers.Base;
using piSensorNet.Logic.Triggers;
using piSensorNet.Logic.TriggerSourceHandlers.Base;
using Module = piSensorNet.DataModel.Entities.Module;
using Timer = System.Timers.Timer;
using static piSensorNet.Common.Helpers.LoggingHelper;

[assembly: InternalsVisibleTo("piSensorNet.Tests")]

namespace piSensorNet.Engine
{
    public delegate void UserFunctionDelegate(IReadOnlyDictionary<string, int> modules);

    internal delegate void MessageHandler(string clientID, int? moduleID, FunctionTypeEnum functionType, bool isQuery, string text, IReadOnlyMap<string, int> moduleAddresses, IpiSensorNetConfiguration moduleConfiguration, IReadOnlyMap<FunctionTypeEnum, int> functionTypes, int? serialProcessID, IHubProxy hubProxy, Action<string> logger);

    internal delegate IReadOnlyMap<string, int> CacheModuleAddressesDelegate(PiSensorNetDbContext context);

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

        private static IpiSensorNetConfiguration Configuration { get; } = ReadOnlyConfiguration.Load("config.json");

        private static volatile bool _doQuit;
        private static volatile bool _pollPartialPackets;
        private static volatile bool _pollPackets;

        // ReSharper disable InconsistentNaming
        private static IReadOnlyMap<string, int> ModuleAddresses;
        private static IReadOnlyMap<FunctionTypeEnum, int> FunctionTypes;
        private static IReadOnlyMap<string, int> FunctionNames;
        private static IReadOnlyDictionary<FunctionTypeEnum, IFunctionHandler> FunctionHandlers;
        private static IReadOnlyDictionary<FunctionTypeEnum, IQueryableFunctionHandler> QueryableFunctionHandlers;
        private static IReadOnlyDictionary<TriggerSourceTypeEnum, IReadOnlyCollection<TriggerSource>> TriggerSources;
        private static IReadOnlyDictionary<TriggerSourceTypeEnum, ITriggerSourceHandler> TriggerSourceHandlers;
        private static IReadOnlyDictionary<TriggerDependencyTypeEnum, ITriggerDependencyHandler> TriggerDependencyHandlers;
        private static IReadOnlyDictionary<int, TriggerDelegate> TriggerDelegates;
        // ReSharper enable InconsistentNaming

        private static readonly ConcurrentQueue<TriggerSource> AbsoluteTimeTriggers = new ConcurrentQueue<TriggerSource>();

        public static int Main(string[] args)
        {
            ToConsole("Main: Initializing Engine...");

            var recreate = args.Any(i => String.Equals(i, "recreate", StringComparison.InvariantCultureIgnoreCase));
            var recreateOnly = args.Any(i => String.Equals(i, "recreateOnly", StringComparison.InvariantCultureIgnoreCase));
            var standalone = args.Any(i => String.Equals(i, "standalone", StringComparison.InvariantCultureIgnoreCase));
            var validateModel = args.Any(i => String.Equals(i, "validateModel", StringComparison.InvariantCultureIgnoreCase));

            standalone = standalone || recreate || recreateOnly || validateModel;

            int? serialProcessID = null;
            if (!standalone)
                serialProcessID = FindSerialProcessID(Configuration, ToConsole);

            var recreateDatabase = (recreate || recreateOnly) && !validateModel;

            PiSensorNetDbContext.Initialize(Configuration.ConnectionString, recreateDatabase);

            //PiSensorNetDbContext.Logger = Console.Write;

            if (validateModel)
            {
                PiSensorNetDbContext.CheckCompatibility(Configuration.ConnectionString);

                ToConsole("Main: Model validation finished, exiting!");

                return 0;
            }

            if (recreateOnly)
            {
                ToConsole("Main: Database recreated, exiting!");

                return 0;
            }

            if (!recreate && !standalone && args.Length > 0)
            {
                ToConsole($"Main: ERROR: Wrong arguments given: '{args.Join(" ")}'.");

                return 1;
            }

            ToConsole("Main: Context initialized!");

            BuildCache();

            //Demo(ModuleConfiguration, FunctionTypes, FunctionNames, FunctionHandlers, QueryableFunctionHandlers, TriggerSourceHandlers, TriggerDelegates, TriggerDependencyHandlers);
            //return 666;

            DisposalQueue toDispose;
            var hubProxy = InitializeHubConnection(Configuration, Configuration, InternalHandleMessage, ModuleAddresses, FunctionTypes, serialProcessID, out toDispose, ToConsole);

            toDispose += Signal.Handle(SignalHandlers);

            var timer = new Timer(1000);
            timer.Elapsed += HandleTimerTick;

            timer.Start();

            ToConsole("Main: Started!");

            while (!_doQuit)
            {
                if (Constants.IsWindows)
                {
                    WaitHandle.WaitOne(1000);

                    using (var context = PiSensorNetDbContext.Connect(Configuration.ConnectionString))
                    {
                        _pollPackets = context.Packets
                                              .Where(i => i.State == PacketStateEnum.New)
                                              .Where(i => i.FunctionID.HasValue)
                                              .Any();

                        _pollPartialPackets = _pollPackets
                                              || context.PartialPackets
                                                        .AsNoTracking()
                                                        .Where(i => i.State == PartialPacketStateEnum.New)
                                                        .Any();
                    }
                }
                else
                    WaitHandle.WaitOne();

                if (_pollPartialPackets)
                {
                    _pollPartialPackets = false;

                    using (var context = PiSensorNetDbContext.Connect(Configuration.ConnectionString))
                    {
                        _pollPackets = MergePackets(context, Configuration, FunctionTypes, FunctionNames, ModuleAddresses, CacheModuleAddresses, ToConsole);

                        if (_pollPackets)
                        {
                            _pollPackets = false;

                            while (HandlePackets(context, Configuration, FunctionTypes, FunctionNames, FunctionHandlers, QueryableFunctionHandlers, TriggerSourceHandlers, TriggerDelegates, TriggerDependencyHandlers, serialProcessID, hubProxy, ToConsole)) {}
                        }
                    }
                }

                HandleAbsoluteTimeTriggers(Configuration, TriggerSourceHandlers, TriggerDelegates, AbsoluteTimeTriggers, ToConsole, TriggerDependencyHandlers);
            }

            ToConsole("Main: Stopping...");

            timer.Stop();
            toDispose.Dispose();

            ToConsole("Main: Stopped!");

            return 0;
        }

        private static void BuildCache()
        {
            using (var context = PiSensorNetDbContext.Connect(Configuration.ConnectionString))
            {
                ModuleAddresses = CacheModuleAddresses(context);

                var cachedFunctions = CacheFunctions(context);
                FunctionTypes = cachedFunctions.Item1;
                FunctionNames = cachedFunctions.Item2;

                var cachedFunctionHandlers = CacheFunctionHandlers();
                FunctionHandlers = cachedFunctionHandlers.Item1;
                QueryableFunctionHandlers = cachedFunctionHandlers.Item2;

                TriggerSourceHandlers = CacheTriggerSourceHandlers();
                TriggerDependencyHandlers = CacheTriggerDependencyHandlers();

                var cachedTriggerSources = CacheTriggerSources(context, TriggerDependencyHandlers);
                TriggerSources = cachedTriggerSources.Item1;
                TriggerDelegates = cachedTriggerSources.Item2;
            }

            ToConsole("BuildCache: Cache built!");
        }

        // ReSharper disable once UnusedMember.Local
        private static void Demo(IpiSensorNetConfiguration moduleConfiguration, IReadOnlyMap<FunctionTypeEnum, int> functionTypes, IReadOnlyMap<string, int> functionNames, IReadOnlyDictionary<FunctionTypeEnum, IFunctionHandler> functionHandlers, IReadOnlyDictionary<FunctionTypeEnum, IQueryableFunctionHandler> queryableFunctionHandlers, IReadOnlyDictionary<TriggerSourceTypeEnum, ITriggerSourceHandler> triggerSourceHandlers, IReadOnlyDictionary<int, TriggerDelegate> triggerDelegates, IReadOnlyDictionary<TriggerDependencyTypeEnum, ITriggerDependencyHandler> triggerDependencyHandlers)
        {
            using (var context = PiSensorNetDbContext.Connect(moduleConfiguration.ConnectionString).WithChangeTracking())
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
                                                          new Packet(i, 0, "identify;function_list;voltage;report;ow_list;ow_ds18b20_temperature;ow_ds18b20_temperature_periodical;", DateTime.Now)
                                                              // "2854280E02000070|25.75;28AC5F2600008030|25.75;"
                                                          {
                                                              FunctionID = functionID,
                                                          }
                                                      }))
                    );

                context.SaveChanges();

                var module = context.Modules.AsNoTracking().Where(i => i.Address == address).Single();
                var packet = context.Packets.Include(i => i.Function).AsNoTracking().Where(i => i.ModuleID == module.ID).OrderByDescending(i => i.Created).First();

                // ReSharper disable once PossibleInvalidOperationException
                var functionHandler = functionHandlers[packet.Function.FunctionType];
                var taskQueue = new HubMessageQueue();

                functionHandler.Handle(new FunctionHandlerContext(moduleConfiguration, context, queryableFunctionHandlers, functionTypes, functionNames, triggerSourceHandlers, triggerDelegates, triggerDependencyHandlers, DateTime.Now), packet, ref taskQueue);

                context.SaveChanges();
            }
        }

        #region Init

        private static int? FindSerialProcessID(IReadOnlyConfiguration configuration, Action<string> logger)
        {
            if (Constants.IsWindows)
                throw new Exception("Linux only!");

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

        private static IHubProxy InitializeHubConnection(IReadOnlyConfiguration configuration, IpiSensorNetConfiguration moduleConfiguration, MessageHandler handler, IReadOnlyMap<string, int> moduleAddresses, IReadOnlyMap<FunctionTypeEnum, int> functionTypes, int? serialProcessID, out DisposalQueue toDispose, Action<string> logger)
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

            toDispose += hubProxy.On<string, int?, FunctionTypeEnum, string>("sendMessage",
                (clientID, moduleID, functionType, text)
                    => handler(clientID, moduleID, functionType, false, text, moduleAddresses, moduleConfiguration, functionTypes, serialProcessID, hubProxy, logger));

            toDispose += hubProxy.On<string, int?, FunctionTypeEnum>("sendQuery",
                (clientID, moduleID, functionType)
                    => handler(clientID, moduleID, functionType, true, null, moduleAddresses, moduleConfiguration, functionTypes, serialProcessID, hubProxy, logger));

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

            toDispose += hubConnection;

            return hubProxy;
        }

        #endregion

        #region Caching

        internal static Tuple<IReadOnlyDictionary<FunctionTypeEnum, IFunctionHandler>, IReadOnlyDictionary<FunctionTypeEnum, IQueryableFunctionHandler>> CacheFunctionHandlers()
        {
            var handlerTypes = ReflectionExtensions.GetImplementations<IFunctionHandler>();

            var functionHandlers = new Dictionary<FunctionTypeEnum, IFunctionHandler>();
            var queryableFunctionHandlers = new Dictionary<FunctionTypeEnum, IQueryableFunctionHandler>();

            foreach (var handlerType in handlerTypes)
            {
                var instance = (IFunctionHandler)Activator.CreateInstance(handlerType);

                functionHandlers.Add(instance.FunctionType, instance);

                var queryableInstance = instance as IQueryableFunctionHandler;
                if (queryableInstance != null)
                    queryableFunctionHandlers.Add(instance.FunctionType, queryableInstance);
            }

            return Tuple.Create(functionHandlers.ReadOnly(), queryableFunctionHandlers.ReadOnly());
        }

        internal static IReadOnlyDictionary<TriggerSourceTypeEnum, ITriggerSourceHandler> CacheTriggerSourceHandlers()
        {
            var handlerTypes = ReflectionExtensions.GetImplementations<ITriggerSourceHandler>();

            var triggerSourceHandlers = handlerTypes
                .Select(Activator.CreateInstance)
                .Cast<ITriggerSourceHandler>()
                .ToDictionary(i => i.TriggerSourceType);

            return triggerSourceHandlers;
        }

        internal static IReadOnlyDictionary<TriggerDependencyTypeEnum, ITriggerDependencyHandler> CacheTriggerDependencyHandlers()
        {
            var handlerTypes = ReflectionExtensions.GetImplementations<ITriggerDependencyHandler>();

            var triggerSourceHandlers = handlerTypes
                .Select(Activator.CreateInstance)
                .Cast<ITriggerDependencyHandler>()
                .ToDictionary(i => i.TriggerDependencyType);

            return triggerSourceHandlers;
        }

        internal static IReadOnlyMap<string, int> CacheModuleAddresses(PiSensorNetDbContext context)
        {
            var modules = context.Modules
                                 .AsNoTracking()
                                 .Select(i => new
                                              {
                                                  i.ID,
                                                  i.Address
                                              })
                                 .ToMap(i => i.Address, i => i.ID);

            return modules;
        }

        internal static Tuple<IReadOnlyMap<FunctionTypeEnum, int>, IReadOnlyMap<string, int>> CacheFunctions(PiSensorNetDbContext context)
        {
            var functions = context.Functions
                                   .AsNoTracking()
                                   .Select(i => new
                                                {
                                                    i.FunctionType,
                                                    i.ID,
                                                    i.Name,
                                                })
                                   .ToList();

            var functionTypes = functions.ToMap(i => i.FunctionType, i => i.ID);
            var functionames = functions.ToMap(i => i.Name, i => i.ID);

            return Tuple.Create(functionTypes.ReadOnly(), functionames.ReadOnly());
        }

        internal static Tuple<IReadOnlyDictionary<TriggerSourceTypeEnum, IReadOnlyCollection<TriggerSource>>, IReadOnlyDictionary<int, TriggerDelegate>> CacheTriggerSources(PiSensorNetDbContext context, IReadOnlyDictionary<TriggerDependencyTypeEnum, ITriggerDependencyHandler> triggerDependencyHandlers)
        {
            // TODO KZ: test
            var triggerSources = context.TriggerSources
                                        .Include(i => i.Trigger)
                                        .AsNoTracking()
                                        .ToList();

            var groupedTriggerSources = triggerSources.GroupBy(i => i.TriggerID)
                                                      .ToDictionary();

            var triggerDependencies = context.TriggerDependencies
                                             .AsNoTracking()
                                             .AsEnumerable()
                                             .GroupBy(i => i.TriggerID)
                                             .ToDictionary();

            triggerSources.Each(i =>
                                {
                                    var trigger = i.Trigger;

                                    if (trigger.TriggerSources.Count == 0)
                                        trigger.TriggerSources.Add(groupedTriggerSources[trigger.ID]);

                                    if (trigger.TriggerDependencies.Count == 0)
                                        trigger.TriggerDependencies.Add(triggerDependencies[trigger.ID]);
                                });

            var typedTriggerSources = triggerSources.GroupBy(i => i.Type)
                                                    .ToDictionary(i => i.ReadOnly());

            var properties = new Dictionary<string, IReadOnlyDictionary<string, Type>>(1)
                             {
                                 {
                                     nameof(TriggerDelegateContext.Properties),
                                     triggerDependencyHandlers.SelectMany(i => i.Value.Properties)
                                                              .ToDictionary()
                                 }
                             };

            var triggerDelegates = new Dictionary<int, TriggerDelegate>();
            foreach (var triggerSource in triggerSources)
            {
                var trigger = triggerSource.Trigger;
                if (triggerDelegates.ContainsKey(trigger.ID))
                    continue;

                var methodCompilationResult = TriggerDelegateCompilerHelper.Compile(properties, trigger.Content);
                if (!methodCompilationResult.IsSuccessful)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    var errors = methodCompilationResult.CompilerErrors.Select(i => $"({i.Line}, {i.Column}) [{i.ErrorNumber}] {i.ErrorText}").Join(Environment.NewLine);

                    throw new Exception($"Compilation of {nameof(Trigger)} #{trigger} failed.{Environment.NewLine}{errors}");
                }

                triggerDelegates.Add(trigger.ID, methodCompilationResult.Method);
            }

            return Tuple.Create(typedTriggerSources.ReadOnly(), triggerDelegates.ReadOnly());
        }

        #endregion

        internal static bool MergePackets(PiSensorNetDbContext context, IpiSensorNetConfiguration moduleConfiguration, IReadOnlyMap<FunctionTypeEnum, int> functionTypes, IReadOnlyMap<string, int> functionNames, IReadOnlyMap<string, int> moduleAddresses, CacheModuleAddressesDelegate cacheModuleAddresses, Action<string> logger)
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

                if (moduleAddresses.Forward.ContainsKey(address) || newModules.ContainsKey(address))
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

                moduleAddresses = cacheModuleAddresses(context);
            }

            logger($"MergePackets: Done creating new modules, found {newModules.Count}!"); // 700us, no modules

            var packetGroupWithPacket = new List<Tuple<Packet, IEnumerable<PartialPacket>>>(groupedPackets.Count);
            foreach (var packetGroup in groupedPackets)
            {
                if (packetGroup.Count() != packetGroup.Key.Total)
                {
                    context.EnqueueUpdate<PartialPacket>(
                        i => i.State == PartialPacketStateEnum.Fragmented,
                        i => packetGroup.Select(ii => ii.ID).Contains(i.ID));

                    continue;
                }

                var moduleID = moduleAddresses.Forward[packetGroup.Key.Address];
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
                var functionID = functionNames.Forward.GetValueOrNullable(functionName);

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
                    context.EnqueueUpdate<PartialPacket>(
                        i => i.PacketID == p.Item1.ID && i.State == PartialPacketStateEnum.Merged,
                        i => p.Item2.Select(ii => ii.ID).Contains(i.ID)));

                context.ExecuteRaw();

                logger("MergePackets: Updated partial packets!"); // ~15ms, 1 packet
            }

            logger($"MergePackets: Done, created {packetGroupWithPacket.Count} packet(s)!");

            return packetGroupWithPacket.Count > 0;
        }

        internal static bool HandlePackets(PiSensorNetDbContext context, IpiSensorNetConfiguration moduleConfiguration, IReadOnlyMap<FunctionTypeEnum, int> functionTypes, IReadOnlyMap<string, int> functionNames, IReadOnlyDictionary<FunctionTypeEnum, IFunctionHandler> functionHandlers, IReadOnlyDictionary<FunctionTypeEnum, IQueryableFunctionHandler> queryableFunctionHandlers, IReadOnlyDictionary<TriggerSourceTypeEnum, ITriggerSourceHandler> triggerSourceHandlers, IReadOnlyDictionary<int, TriggerDelegate> triggerDelegates, IReadOnlyDictionary<TriggerDependencyTypeEnum, ITriggerDependencyHandler> triggerDependencyHandlers, int? serialProcessID, IHubProxy hubProxy, Action<string> logger)
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

            var handlerContext = new FunctionHandlerContext(moduleConfiguration, context, queryableFunctionHandlers, functionTypes, functionNames, triggerSourceHandlers, triggerDelegates, triggerDependencyHandlers, DateTime.Now);

            var handleAgain = false;
            var newMesagesAdded = false;
            var hubTasksQueue = new HubMessageQueue();
            foreach (var packet in packets)
            {
                // ReSharper disable once PossibleInvalidOperationException
                var handler = functionHandlers.GetValueOrDefault(packet.Function.FunctionType);
                if (handler == null)
                {
                    context.EnqueueUpdate<Packet>(
                        i => i.State == PacketStateEnum.Unhandled,
                        i => i.ID == packet.ID);

                    logger($"HandlePackets: Packet #{packet.ID} (function '{packet.Function.FunctionType}') could not be handled, no handler found!");

                    continue;
                }

                var functionHandlerResult = handler.IsModuleIdentityRequired && packet.Module.State != ModuleStateEnum.Identified
                    ? PacketStateEnum.Skipped
                    : handler.Handle(handlerContext, packet, ref hubTasksQueue);

                handleAgain = handleAgain || functionHandlerResult.ShouldHandlePacketsAgain;
                newMesagesAdded = newMesagesAdded || functionHandlerResult.NewMessagesAdded;

                context.EnqueueUpdate<Packet>(
                    i => i.State == functionHandlerResult.PacketState && i.Processed == DateTime.Now,
                    i => i.ID == packet.ID);

                if (handler.TriggerSourceType.HasValue && functionHandlerResult.PacketState == PacketStateEnum.Handled)
                    TriggerSources[handler.TriggerSourceType.Value].Each(i =>
                        TriggerSourceHandlerHelper.Handle(handlerContext, i, packet.Module.ID));

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
                hubProxy.SafeInvoke(hubTasksQueue.Dequeue());

            logger($"HandlePackets: Hub message(s) sent{(newMesagesAdded ? ", Serial signaled about new message(s)" : String.Empty)}{(handleAgain ? ", packet(s) will be handled again" : String.Empty)}!"); // ~10ms

            return handleAgain;
        }

        internal static void HandleMessage(string clientID, int? moduleID, FunctionTypeEnum functionType, bool isQuery, string text, IReadOnlyMap<string, int> moduleAddresses, IpiSensorNetConfiguration moduleConfiguration, IReadOnlyMap<FunctionTypeEnum, int> functionTypes, IHubProxy hubProxy, Action<string> logger)
        {
            logger($"HandleMessage: Received message from ${clientID} to @{moduleID?.ToString() ?? "ALL"} - {functionType}{(text == null ? (isQuery ? "?" : String.Empty) : ":" + text)}");

            if (moduleID.HasValue && !moduleAddresses.Reverse.ContainsKey(moduleID.Value))
            {
                hubProxy.Invoke("error", clientID, $"Module #{moduleID.Value} does not exist.");

                logger($"HandleMessage: ERROR: Message not handled, module #{moduleID.Value} does not exist!");

                return;
            }

            int messageID;
            using (var context = PiSensorNetDbContext.Connect(moduleConfiguration.ConnectionString))
            {
                var message = new Message(functionTypes.Forward[functionType], isQuery)
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

        internal static void HandleAbsoluteTimeTriggers(IpiSensorNetConfiguration moduleConfiguration, IReadOnlyDictionary<TriggerSourceTypeEnum, ITriggerSourceHandler> triggerHandlers, IReadOnlyDictionary<int, TriggerDelegate> triggerDelegates, ConcurrentQueue<TriggerSource> absoluteTimeTriggers, Action<string> logger, IReadOnlyDictionary<TriggerDependencyTypeEnum, ITriggerDependencyHandler> triggerDependencyHandlers)
        {
            if (absoluteTimeTriggers.Count == 0)
                return;

            logger("HandleAbsoluteTimeTriggers: Start...");

            using (var context = PiSensorNetDbContext.Connect(moduleConfiguration.ConnectionString))
            {
                var handlerContext = new TriggerSourceHandlerHelperContext(context, triggerHandlers, triggerDelegates, triggerDependencyHandlers, DateTime.Now);

                while (absoluteTimeTriggers.Count > 0)
                {
                    TriggerSource triggerSource;

                    if (absoluteTimeTriggers.TryDequeue(out triggerSource))
                        continue;

                    logger($"HandleAbsoluteTimeTriggers: Handling trigger source #{triggerSource.ID}...");

                    TriggerSourceHandlerHelper.Handle(handlerContext, triggerSource);
                }
            }

            logger($"HandleAbsoluteTimeTriggers: Finished handling {absoluteTimeTriggers.Count} trigger source(s)!");
        }

        private static void HandleTimerTick(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            var now = DateTime.Now;
            var triggerSources = TriggerSources[TriggerSourceTypeEnum.AbsoluteTime]
                .Where(i => !i.NextAbsoluteTimeExecution.HasValue
                            || i.NextAbsoluteTimeExecution.Value < now)
                .ToList();

            if (triggerSources.Count == 0)
                return;

            foreach (var triggerSource in triggerSources)
                AbsoluteTimeTriggers.Enqueue(triggerSource);

            WaitHandle.Set();
        }

        private static void InternalHandleMessage(string clientID, int? moduleID, FunctionTypeEnum functionType, bool isQuery, string text, IReadOnlyMap<string, int> modules, IpiSensorNetConfiguration moduleConfiguration, IReadOnlyMap<FunctionTypeEnum, int> functionTypes, int? serialProcessID, IHubProxy hubProxy, Action<string> logger)
        {
            HandleMessage(clientID, moduleID, functionType, isQuery, text, modules, moduleConfiguration, functionTypes, hubProxy, logger);

            if (serialProcessID.HasValue)
                Signal.Send(serialProcessID.Value, SignalTypeEnum.User1);
        }

        #region Signal Handlers

        private static void QuitSignalHandler(SignalTypeEnum signalType)
        {
            ToConsole($"Received quit signal as '{signalType}'!");

            _doQuit = true;
            WaitHandle.Set();
        }

        private static void NewPartialPacketsToMergeSignalHandler(SignalTypeEnum signalType)
        {
            ToConsole("Received signal to process new message!");

            _pollPartialPackets = true;
            WaitHandle.Set();
        }

        private static void RedoCacheSignalHandler(SignalTypeEnum signalType)
        {
            ToConsole("Received signal to redo cache!");

            BuildCache();
        }

        #endregion
    }
}