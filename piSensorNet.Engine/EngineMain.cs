using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using piSensorNet.Common;
using piSensorNet.Common.Custom;
using piSensorNet.Common.Extensions;
using piSensorNet.DataModel.Context;
using piSensorNet.DataModel.Entities;
using piSensorNet.DataModel.Enums;
using piSensorNet.DataModel.Extensions;
using piSensorNet.Common.System;
using piSensorNet.Logic.FunctionHandlers.Base;

[assembly:InternalsVisibleTo("piSensorNet.Tests")]

namespace piSensorNet.Engine
{
    public static class EngineMain
    {
        private static readonly EventWaitHandle WaitHandle = new AutoResetEvent(false);

        private static readonly IReadOnlyDictionary<SignalTypeEnum, Action<SignalTypeEnum>> SignalHandlers =
            new Dictionary<SignalTypeEnum, Action<SignalTypeEnum>>
            {
                {SignalTypeEnum.Quit, QuitSignalHandler},
                {SignalTypeEnum.Interrupt, QuitSignalHandler},
                {SignalTypeEnum.User1, NewMessageToProcessSignalHandler},
            };


        private static IConfiguration Configuration { get; } = Common.Configuration.Load("config.json");
        private static IModuleConfiguration ModuleConfiguration { get; } = new ModuleConfiguration(Configuration);

        internal static readonly IReadOnlyCollection<FunctionTypeEnum> DefaultFunctions
            = Configuration["Settings:DefaultFunctions"].Split(',')
                                                        .Select(i => Enum.Parse(Reflector.Instance<FunctionTypeEnum>.Type, i))
                                                        .Cast<FunctionTypeEnum>()
                                                        .ToList();

        private static int? SerialProcessID;

        private static volatile bool _doQuit;
        private static volatile bool _pollReceivedMessages;
        private static volatile bool _pollPackets;

        private static readonly Action<string> Logger = i => Console.WriteLine($"{DateTime.Now.ToString("O")}: {i}");

        private static IReadOnlyDictionary<string, int> Modules;
        private static IReadOnlyDictionary<int, string> InverseModules;
        private static IReadOnlyDictionary<int, IFunctionHandler> FunctionHandlers;
        private static IReadOnlyDictionary<string, IQueryableFunctionHandler> QueryableFunctionHandlers;
        private static IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> Functions;
        private static IReadOnlyDictionary<string, int> InverseFunctions;

        private static readonly StringBuilder MessageBuilder = new StringBuilder(200);

        public static int Main(string[] args)
        {
            Logger("Initializing Engine...");

            var recreate = args.Any(i => String.Equals(i, "recreate", StringComparison.InvariantCultureIgnoreCase));
            var recreateOnly = args.Any(i => String.Equals(i, "recreateOnly", StringComparison.InvariantCultureIgnoreCase));
            var standalone = args.Any(i => String.Equals(i, "standalone", StringComparison.InvariantCultureIgnoreCase));

            if (!standalone)
                SerialProcessID = FindSerialProcessID(Configuration, recreate || recreateOnly, Logger);

            PiSensorNetDbContext.Initialize(ModuleConfiguration.ConnectionString, recreate || recreateOnly);
            
            //PiSensorNetDbContext.Logger = Console.Write;

            if (recreateOnly)
            {
                Logger("Database recreated, exiting!");

                return 0;
            }

            if (!recreate && !standalone && args.Length > 0)
            {
                Logger($"Wrong arguments given: '{args.Join(" ")}'.");

                return 1;
            }

            Logger("Context initialized!");

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

            //Demo();
            //return 666;

            var handler = Signal.Handle(SignalHandlers);

            Logger("Cache built!");

            DisposalQueue toDispose;
            var hubProxy = InitializeHubConnection(Configuration, ModuleConfiguration, InternalHandleMessage, InverseModules, Functions, MessageBuilder, out toDispose, Logger);

            Logger("Started!");

            while (!_doQuit)
            {
                if (Constants.IsWindows)
                    WaitHandle.WaitOne(500);
                else
                    WaitHandle.WaitOne();

                if (_pollReceivedMessages || Constants.IsWindows)
                {
                    _pollReceivedMessages = false;

                    using (var context = PiSensorNetDbContext.Connect(ModuleConfiguration.ConnectionString))
                    {
                        PollReceivedMessagesAndCreatePartialPackets(context, ModuleConfiguration, Logger);
                        _pollPackets = MergePackets(context, ModuleConfiguration, DefaultFunctions, Functions, InverseFunctions, Modules, hubProxy, InternalCacheModules, Logger);

                        if (_pollPackets)
                        {
                            _pollPackets = false;

                            HandlePackets(context, ModuleConfiguration, Functions, FunctionHandlers, QueryableFunctionHandlers, hubProxy, Logger);
                        }
                    }
                }
            }

            Logger("Stopping...");

            toDispose?.Dispose();
            handler.Dispose();

            Logger("Stopped!");

            return 0;
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
                var taskQueue = new Queue<Func<IHubProxy, Task>>();

                functionHandler.Handle(ModuleConfiguration, context, packet, QueryableFunctionHandlers, Functions, taskQueue);

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

            logger($"Searching for ID of process with name fragment '{nameFragment}'...");

            var pid = Processess.FindByFragment(nameFragment, StringComparison.InvariantCulture, "sudo");
            if (!pid.HasValue)
            {
                logger($"ERROR: Serial process with name fragment '{nameFragment}' not found!");
                Environment.Exit(-1);
            }

            logger($"Found process {pid.Value}!");

            return pid.Value;
        }

        private static IHubProxy InitializeHubConnection(IConfiguration configuration, IModuleConfiguration moduleConfiguration, Action<string, int?, FunctionTypeEnum, bool, string, IReadOnlyDictionary<int, string>, IModuleConfiguration, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>>, StringBuilder, IHubProxy, Action<string>> handler, IReadOnlyDictionary<int, string> inverseModules, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> functions, StringBuilder builder, out DisposalQueue toDispose, Action<string> logger)
        {
            toDispose = new DisposalQueue();

            var hubConnection = new HubConnection(configuration["Settings:WebAddress"],
                new Dictionary<string, string>
                {
                    {
                        configuration["Settings:SignalREngineFlagName"], true.ToString().ToLowerInvariant()
                    }
                });

            var hubProxy = hubConnection.CreateHubProxy(configuration["Settings:SignalRHubName"]);

            toDispose.Enqueue(hubProxy.On<string, int?, FunctionTypeEnum, string>("sendMessage",
                (clientID, moduleID, functionType, text)
                    => handler(clientID, moduleID, functionType, false, text, inverseModules, moduleConfiguration, functions, builder, hubProxy, logger)));

            toDispose.Enqueue(hubProxy.On<string, int?, FunctionTypeEnum>("sendQuery",
                (clientID, moduleID, functionType)
                    => handler(clientID, moduleID, functionType, true, null, inverseModules, moduleConfiguration, functions, builder, hubProxy, logger)));

            try
            {
                hubConnection.Start().Wait();
            }
            catch (Exception e)
            {
                logger($"Error while initializing hub connection: {e.Message}.");

                toDispose = null;
                return null;
            }

            logger($"Connection to hub started, ID {hubConnection.ConnectionId}!");

            toDispose.Enqueue(hubConnection);

            return hubProxy;
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

        // TODO KZ: test
        internal static void HandlePackets(PiSensorNetDbContext context, IModuleConfiguration moduleConfiguration, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> functions, IReadOnlyDictionary<int, IFunctionHandler> functionHandlers, IReadOnlyDictionary<string, IQueryableFunctionHandler> queryableFunctionHandlers, IHubProxy hubProxy, Action<string> logger)
        {
            logger("Handling packets...");
            
            var packets = context.Packets
                                 .Include(i => i.Module)
                                 .Include(i => i.Function)
                                 .Where(i => i.State == PacketStateEnum.New)
                                 .Where(i => i.FunctionID.HasValue)
                                 .OrderBy(i => i.Received)
                                 .ToList();

            if (packets.Count == 0)
                return;

            var hubTasksQueue = new Queue<Func<IHubProxy, Task>>();
            var sqls = new StringBuilder(200);
            foreach (var packet in packets)
            {
                // ReSharper disable once PossibleInvalidOperationException
                var handler = functionHandlers.GetValueOrDefault(packet.FunctionID.Value);
                if (handler == null)
                {
                    sqls.AppendLine(Packet.GenerateUpdate(
                        context.GetTableName<Packet>(),
                        new Dictionary<Expression<Func<Packet, object>>, string>
                        {
                            {i => i.State, PacketStateEnum.Unhandled.ToSql()}
                        },
                        new KeyValuePair<Expression<Func<Packet, object>>, string>(i => i.ID, packet.ID.ToSql())));

                    continue;
                }

                handler.Handle(moduleConfiguration, context, packet, queryableFunctionHandlers, functions, hubTasksQueue);

                sqls.AppendLine(Packet.GenerateUpdate(
                    context.GetTableName<Packet>(),
                    new Dictionary<Expression<Func<Packet, object>>, string>
                    {
                        {i => i.State, PacketStateEnum.Handled.ToSql()},
                        {i => i.Handled, DateTime.Now.ToSql()}
                    },
                    new KeyValuePair<Expression<Func<Packet, object>>, string>(i => i.ID, packet.ID.ToSql())));
            }

            logger($"Done handling packets, found {packets.Count}!"); // ~22ms

            while (hubTasksQueue.Count > 0)
                hubTasksQueue.Dequeue()(hubProxy);
            
            logger("Hub messages sent!"); // ~800ms
        }

        internal static void PollReceivedMessagesAndCreatePartialPackets(PiSensorNetDbContext context, IModuleConfiguration moduleConfiguration, Action<string> logger)
        {
            logger("Polling received messages...");

            var receivedMessages = context.ReceivedMessages
                                          .AsNoTracking()
                                          .Where(i => i.IsPacket)
                                          .Where(i => !i.HasPartialPacket)
                                          .OrderBy(i => i.Received)
                                          .ToList();

            logger("Done selecting..."); // ~14ms

            if (receivedMessages.Count == 0)
                return;

            foreach (var receivedMessage in receivedMessages)
            {
                var text = receivedMessage.Text;

                var partialPacket = new PartialPacket(
                    receivedMessage.ID,
                    String.Format(moduleConfiguration.AddressPattern, text.SubstringBetween("@", "#")),
                    Byte.Parse(text.SubstringBetween("#", "(")),
                    Byte.Parse(text.SubstringBetween("(", "/")),
                    Byte.Parse(text.SubstringBetween("/", ")")),
                    text.SubstringAfter("):"),
                    receivedMessage.Received);

                context.PartialPackets.Add(partialPacket);
            }

            //logger("Done creating partial packets..."); // ~3ms

            context.SaveChanges();

            logger("Done saving changes..."); // ~158ms

            var updateReceivedMessagesSql = $"UPDATE `{context.GetTableName<ReceivedMessage>()}` " +
                                            $"SET `{PiSensorNetDbContext.GetMemberName<ReceivedMessage>(i => i.HasPartialPacket)}` = {true.ToSql()} " +
                                            $"WHERE `{PiSensorNetDbContext.GetMemberName<ReceivedMessage>(i => i.ID)}` IN ({receivedMessages.Select(i => i.ID.ToString()).Join(", ")}); ";

            context.Database.ExecuteSqlCommand(updateReceivedMessagesSql);

            logger($"Done polling received messages, processed {receivedMessages.Count} partial packets!"); // ~22ms
        }

        internal static bool MergePackets(PiSensorNetDbContext context, IModuleConfiguration moduleConfiguration, IReadOnlyCollection<FunctionTypeEnum> defaultFunctionTypes, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> functions, IReadOnlyDictionary<string, int> inverseFunctions, IReadOnlyDictionary<string, int> modules, IHubProxy hubProxy, Func<PiSensorNetDbContext, Tuple<IReadOnlyDictionary<string, int>, IReadOnlyDictionary<int, string>>> cacheModules, Action<string> logger)
        {
            logger("Merging packets...");

            var partialPackets = context.PartialPackets
                                        .AsNoTracking()
                                        .Where(i => !i.PacketID.HasValue)
                                        .ToList();

            logger("Done selecting..."); // ~12ms

            var groupedPackets = partialPackets.GroupBy(i => new
                                                             {
                                                                 i.Address,
                                                                 i.Number,
                                                                 i.Total
                                                             })
                                               .ToList();

            //logger("Done grouping..."); // <1ms

            var newModules = new Dictionary<string, Module>();
            var defaultFunctions = new Lazy<ICollection<Function>>(() => context.Functions.Where(i => defaultFunctionTypes.Contains(i.FunctionType)).ToList());
            foreach (var packetGroup in groupedPackets)
            {
                var address = packetGroup.Key.Address;

                if (modules.ContainsKey(address) || newModules.ContainsKey(address))
                    continue;

                var module = new Module(address);

                context.Modules.Add(module);

                context.ModuleFunctions.AddRange(defaultFunctions.Value.Select(i => new ModuleFunction(module, i)));

                context.Messages.Add(new Message(functions[FunctionTypeEnum.Report].Key, false)
                                     {
                                         Module = module
                                     });

                newModules.Add(address, module);
            }

            logger("Done creating modules..."); // <1ms

            if (newModules.Count > 0)
            {
                context.SaveChanges();

                logger("Done saving modules...");

                modules = cacheModules(context).Item1;

                logger("Done caching modules...");

                foreach (var newModule in newModules)
                    hubProxy.Invoke("newModule", newModule.Value.ID, newModule.Value.Address);
            }

            logger($"Done creating new modules, found {newModules.Count}!"); // <1ms, no modules

            var packetGroupWithPacket = new List<Tuple<string, Packet>>(groupedPackets.Count);
            foreach (var packetGroup in groupedPackets)
            {
                if (packetGroup.Count() != packetGroup.Key.Total)
                    continue;

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

                packetGroupWithPacket.Add(Tuple.Create(packetGroup.Select(i => i.ReceivedMessageID.ToString()).Join(", "), packet));
            }

            if (packetGroupWithPacket.Count > 0)
            {
                logger("Done parsing packet groups..."); // ~3ms

                context.SaveChanges();

                logger("Done saving changes..."); // ~23ms

                context.Database.ExecuteSqlCommand(packetGroupWithPacket.Select(p =>
                    $"UPDATE `{context.GetTableName<PartialPacket>()}` " +
                    $"SET `{PiSensorNetDbContext.GetMemberName<PartialPacket>(i => i.PacketID)}` = {p.Item2.ID} " +
                    $"WHERE `{PiSensorNetDbContext.GetMemberName<PartialPacket>(i => i.ReceivedMessageID)}` IN ({p.Item1}); ")
                                                                        .Concat());
            }

            logger($"Done merging packets, created {packetGroupWithPacket.Count}!"); // ~20ms, 1 packet
            
            return packetGroupWithPacket.Count > 0;
        }
        
        //private static void AssembleSentMessage(PiSensorNetDbContext context, StringBuilder builder, Message messageToSend, string functionName, IReadOnlyDictionary<int, string> inverseModules, IModuleConfiguration moduleConfiguration)
        //{
        //    var nodeAddress = messageToSend.ModuleID.HasValue ? inverseModules[messageToSend.ModuleID.Value] : moduleConfiguration.BroadcastAddress;
        //    var messageID = messageToSend.ID.ToBase36();

        //    builder.Clear();

        //    builder.Append(nodeAddress);
        //    builder.Append(moduleConfiguration.AddressDelimiter);
        //    builder.Append(messageID);
        //    builder.Append(moduleConfiguration.MessageIDDelimiter);
        //    builder.Append(functionName);

        //    if (messageToSend.IsQuery)
        //        builder.Append(moduleConfiguration.FunctionQuery);
        //    else if (messageToSend.Text != null)
        //    {
        //        builder.Append(moduleConfiguration.FunctionDelimiter);
        //        builder.Append(messageToSend.Text);
        //    }

        //    var text = builder.ToString();

        //    context.SentMessages.Add(new SentMessage(messageToSend.ID, text));
        //}
        
        #region Handlers

        internal static void HandleMessage(string clientID, int? moduleID, FunctionTypeEnum functionType, bool isQuery, string text, IReadOnlyDictionary<int, string> inverseModules, IModuleConfiguration moduleConfiguration, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> functions, StringBuilder builder, IHubProxy hubProxy, Action<string> logger)
        { // ~70ms
            logger($"Handling message from ${clientID} to @{moduleID ?? -2} - {functionType} {text ?? (isQuery ? "?" : String.Empty)}");

            if (moduleID.HasValue && !inverseModules.ContainsKey(moduleID.Value))
            {
                hubProxy.Invoke("error", $"Module #{moduleID.Value} does not exist.");

                logger($"ERROR: Message not handled, module #{moduleID.Value} does not exist!");

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
                
                //AssembleSentMessage(context, builder, message, functions[functionType].Value, inverseModules, moduleConfiguration);

                //context.SaveChanges();

                messageID = message.ID;
            }

            logger($"Message handled to #{messageID}!");
        }

        private static void InternalHandleMessage(string clientID, int? moduleID, FunctionTypeEnum functionType, bool isQuery, string text, IReadOnlyDictionary<int, string> inverseModules, IModuleConfiguration moduleConfiguration, IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> functions, StringBuilder builder, IHubProxy hubProxy, Action<string> logger)
        {
            HandleMessage(clientID, moduleID, functionType, isQuery, text, inverseModules, moduleConfiguration, functions, builder, hubProxy, logger);

            if (SerialProcessID.HasValue)
                Signal.Send(SerialProcessID.Value, SignalTypeEnum.User1);
        }

        private static void QuitSignalHandler(SignalTypeEnum signalType)
        {
            Logger($"Received quit signal as '{signalType}'!");

            _doQuit = true;
            WaitHandle.Set();
        }

        private static void NewMessageToProcessSignalHandler(SignalTypeEnum signalType)
        {
            Logger("Received new message to process signal!");

            _pollReceivedMessages = true;
            WaitHandle.Set();
        }

        #endregion
    }
}
