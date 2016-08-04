using System;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using piSensorNet.Common;
using piSensorNet.Common.Extensions;
using piSensorNet.Common.System;
using piSensorNet.DataModel.Context;
using piSensorNet.DataModel.Entities;
using piSensorNet.DataModel.Enums;
using piSensorNet.DataModel.Extensions;
using piSensorNet.WiringPi.Managed;
using piSensorNet.WiringPi.Managed.Enums;

[assembly: InternalsVisibleTo("piSensorNet.Tests")]

namespace piSensorNet.Serial
{
    public static class SerialMain
    {
        private static readonly EventWaitHandle WaitHandle = new AutoResetEvent(false);

        private static readonly IReadOnlyDictionary<SignalTypeEnum, Action<SignalTypeEnum>> SignalHandlers =
            new Dictionary<SignalTypeEnum, Action<SignalTypeEnum>>
            {
                {SignalTypeEnum.Quit, QuitSignalHandler},
                {SignalTypeEnum.Interrupt, QuitSignalHandler},
                {SignalTypeEnum.User1, NeMessageToSendSignalHandler},
            };

        private static IConfiguration Configuration { get; } = Common.Configuration.Load("config.json");
        private static IModuleConfiguration ModuleConfiguration { get; } = new ModuleConfiguration(Configuration);

        private static int? _lastMessageSentID;

        private static readonly ConcurrentQueue<string> ReceivedMessages = new ConcurrentQueue<string>();
        private static readonly StringBuilder Buffer = new StringBuilder(1024);
        private static readonly Queue<Message> MessagesToSend = new Queue<Message>(10);
        private static readonly StringBuilder MessageBuilder = new StringBuilder(200);

        private static volatile int _readSerial;
        private static volatile bool _doQuit;
        private static volatile bool _pollMessagesToSend;

        private static readonly Action<string> Logger = i => Console.WriteLine($"{DateTime.Now.ToString("O")}: {i}");

        public static int Main(string[] args)
        {
            Logger("Initializing Serial Monitor..");

            var engineProcessID = FindSerialProcessID(Configuration, Logger);

            PiSensorNetDbContext.Initialize(ModuleConfiguration.ConnectionString);

            //PiSensorNetDbContext.Logger = Console.Write;

            Logger("Context initialized!");

            var signalHandler = Signal.Handle(SignalHandlers);

            Pi.Serial.Open();

            Pi.Pins.Setup(BroadcomPinNumberEnum.Gpio18, PinModeEnum.Input, PullUpModeEnum.Up);
            var interruptHandler = Pi.Interrupts.SetupPolled(BroadcomPinNumberEnum.Gpio18, InterruptModeEnum.FallingEdge, SerialInterruptHandler);

            Logger("Started!");

            while (!_doQuit)
            {
                WaitHandle.WaitOne(_readSerial == 0 ? -1 : 3);

                if (_readSerial > 0)
                {
                    --_readSerial;
                    if (ReadSerial(ReceivedMessages, Buffer, Logger))
                    {
                        ++_readSerial;
                        continue;
                    }

                    if (_readSerial > 0)
                        continue;
                }

                HandleReceivedMessages(engineProcessID, ReceivedMessages, ModuleConfiguration, Logger);

                if (_pollMessagesToSend)
                {
                    _pollMessagesToSend = false;
                    PollMessagesToSend(MessagesToSend, ModuleConfiguration, Logger);
                }

                _lastMessageSentID = SendMessage(MessagesToSend, _lastMessageSentID, ModuleConfiguration, MessageBuilder, Logger);
            }

            Logger("Stopping...");

            interruptHandler.Dispose();
            Pi.Interrupts.Remove(BroadcomPinNumberEnum.Gpio18);

            Pi.Serial.Flush();
            Pi.Serial.Close();

            signalHandler.Dispose();

            Logger("Stopped!");

            return 0;
        }

        private static int? FindSerialProcessID(IConfiguration configuration, Action<string> logger)
        {
            if (Constants.IsWindows)
                throw new Exception("Linux only!");

            var nameFragment = configuration["Settings:EngineProcessNameFragment"];

            logger($"Searching for ID of process with name fragment '{nameFragment}'...");

            var pid = Processess.FindByFragment(nameFragment, StringComparison.InvariantCulture, "sudo");
            if (!pid.HasValue)
            {
                logger($"ERROR: Engine process with name fragment '{nameFragment}' not found!");
                Environment.Exit(-1);
            }

            logger($"Found process {pid.Value}!");

            return pid.Value;
        }

        private static bool ReadSerial(ConcurrentQueue<string> receivedMessages, StringBuilder buffer, Action<string> logger)
        {
            var read = false;

            var availableCharacters = Pi.Serial.GetAvailableDataCount();
            while (availableCharacters > 0)
            {
                read = true;

                for (var i = 0; i < availableCharacters; ++i)
                {
                    var readCharacter = Pi.Serial.Get();
                    if (readCharacter == Pi.Serial.Terminator)
                    {
                        var item = buffer.ToString();

                        logger($"Enqueuing item read from serial: '{item}'!");

                        receivedMessages.Enqueue(item);
                        buffer.Clear();

                        continue;
                    }

                    if (readCharacter == '\r')
                        continue;

                    buffer.Append(readCharacter);
                }

                availableCharacters = Pi.Serial.GetAvailableDataCount();
            }

            return read;
        }

        internal static void HandleReceivedMessages(int? engineProcessID, ConcurrentQueue<string> receivedMessages, IModuleConfiguration moduleConfiguration, Action<string> logger)
        {
            if (receivedMessages.Count == 0)
                return;

            logger("Handling received messages...");

            var arePacketsProcessed = false;
            using (var context = PiSensorNetDbContext.Connect(moduleConfiguration.ConnectionString))
            {
                while (receivedMessages.Count > 0)
                {
                    logger("Dequeing...");

                    string text;
                    if (!receivedMessages.TryDequeue(out text))
                        continue;

                    logger($"Processing '{text}'...");

                    var isFailed = text.StartsWith("FAIL ", StringComparison.InvariantCultureIgnoreCase);
                    var isOk = text.StartsWith("OK ", StringComparison.InvariantCultureIgnoreCase);

                    if (isFailed || isOk)
                    {
                        if (!_lastMessageSentID.HasValue)
                        {
                            logger($"! Message '{text} was not handled due to lack of {nameof(_lastMessageSentID)}!");
                            continue;
                        }

                        var state = isOk ? MessageStateEnum.Completed : MessageStateEnum.Failed;
                        var error = isFailed ? text.Substring("FAIL ".Length) : null;

                        context.EnqueueQuery(Message.GenerateUpdate(context,
                            new Dictionary<Expression<Func<Message, object>>, string>
                            {
                                {i => i.State, state.ToSql()},
                                {i => i.ResponseReceived, DateTime.Now.ToSql()},
                                {i => i.Error, error.ToSql()},
                            },
                            new Tuple<Expression<Func<Message, object>>, string, string>(i => i.ID, "=", _lastMessageSentID.Value.ToSql())));

                        logger($"Updated message #{_lastMessageSentID.Value} with state '{state}'!");

                        _lastMessageSentID = null;
                    }
                    else
                    {
                        var partialPacket = new PartialPacket(
                            String.Format(moduleConfiguration.AddressPattern, text.SubstringBetween("@", "#")),
                            Byte.Parse(text.SubstringBetween("#", "(")),
                            Byte.Parse(text.SubstringBetween("(", "/")),
                            Byte.Parse(text.SubstringBetween("/", ")")),
                            text.SubstringAfter("):"),
                            DateTime.Now);

                        context.PartialPackets.Add(partialPacket);

                        arePacketsProcessed = true;

                        logger("Processed message to partial packet!");
                    }
                }

                context.SaveChanges();

                logger("Messages handled!"); // ~50ms
            }

            if (arePacketsProcessed && engineProcessID.HasValue)
                Signal.Send(engineProcessID.Value, SignalTypeEnum.User1);
        }

        private static void PollMessagesToSend(Queue<Message> messagesToSend, IModuleConfiguration moduleConfiguration, Action<string> logger)
        { // ~24ms, 1 message
            logger("Polling messages to send...");

            using (var context = PiSensorNetDbContext.Connect(moduleConfiguration.ConnectionString))
            {
                var messages = context.Messages
                                      .Include(i => i.Function)
                                      .Include(i => i.Module)
                                      .AsNoTracking()
                                      .Where(i => i.State == MessageStateEnum.Queued)
                                      .OrderBy(i => i.Created)
                                      .ToList();

                if (messages.Count == 0)
                    return;

                foreach (var message in messages)
                    messagesToSend.Enqueue(message);

                logger($"Enqueued {messages.Count} message(s) to send!");
            }
        }

        private static string AssembleMessage(StringBuilder builder, Message messageToSend, IModuleConfiguration moduleConfiguration)
        {
            var nodeAddress = messageToSend.Module?.Address ?? moduleConfiguration.BroadcastAddress;
            var messageID = messageToSend.ID.ToBase36();

            builder.Clear();

            builder.Append(nodeAddress);
            builder.Append(moduleConfiguration.AddressDelimiter);
            builder.Append(messageID);
            builder.Append(moduleConfiguration.MessageIDDelimiter);
            builder.Append(messageToSend.Function.Name);

            if (messageToSend.IsQuery)
                builder.Append(moduleConfiguration.FunctionQuery);
            else if (messageToSend.Text != null)
            {
                builder.Append(moduleConfiguration.FunctionDelimiter);
                builder.Append(messageToSend.Text);
            }

            var text = builder.ToString();

            return text;
        }

        private static int? SendMessage(Queue<Message> messagesToSend, int? lastMessageSentID, IModuleConfiguration moduleConfiguration, StringBuilder messageBuilder, Action<string> logger)
        {
            if (messagesToSend.Count == 0 || lastMessageSentID.HasValue)
                return lastMessageSentID;

            logger("Send message...");

            var messageToSend = messagesToSend.Dequeue();
            var text = AssembleMessage(messageBuilder, messageToSend, moduleConfiguration);

            logger($"Putting '{text}'..."); // ~300us

            Pi.Serial.Put(text);

            logger("Put!");

            using (var context = PiSensorNetDbContext.Connect(moduleConfiguration.ConnectionString))
            {
                context.EnqueueQuery(Message.GenerateUpdate(context,
                    new Dictionary<Expression<Func<Message, object>>, string>
                    {
                        {i => i.State, MessageStateEnum.Sent.ToSql()},
                        {i => i.Sent, DateTime.Now.ToSql()},
                    },
                    new Tuple<Expression<Func<Message, object>>, string, string>(i => i.ID, "=", messageToSend.ID.ToSql())));

                context.ExecuteQueries();
            }

            logger("Message sent!"); // ~20ms

            return messageToSend.ID;
        }

        #region Handlers

        private static void SerialInterruptHandler()
        {
            Logger("Received Serial Interrupt!");

            ++_readSerial;
            WaitHandle.Set();
        }

        private static void QuitSignalHandler(SignalTypeEnum signalType)
        {
            Logger($"Received quit signal as '{signalType}'!");

            _doQuit = true;
            WaitHandle.Set();
        }

        private static void NeMessageToSendSignalHandler(SignalTypeEnum signalType)
        {
            Logger("Received new message signal!");

            _pollMessagesToSend = true;
            WaitHandle.Set();
        }

        #endregion

    }
}