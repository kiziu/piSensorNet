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

        private static int? EngineProcessID;

        private static int? _lastMessageSentID;

        private static readonly ConcurrentQueue<string> ReceivedMessages = new ConcurrentQueue<string>();
        private static readonly StringBuilder Buffer = new StringBuilder(1024);

        private static readonly Queue<Message> MessagesToSend = new Queue<Message>(10);
        //private static readonly Queue<SentMessage> MessagesToSend = new Queue<SentMessage>(10);

        private static volatile int _readSerial;
        private static volatile bool _doQuit;
        private static volatile bool _pollMessagesToSend;

        private static readonly Action<string> Logger = i => Console.WriteLine($"{DateTime.Now.ToString("O")}: {i}");

        private static readonly StringBuilder MessageBuilder = new StringBuilder(200);

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

        public static int Main(string[] args)
        {
            Logger("Initializing Serial Monitor..");

            EngineProcessID = FindSerialProcessID(Configuration, Logger);

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

                HandleReceivedMessages(ReceivedMessages, ModuleConfiguration, Logger);

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

        private static bool ReadSerial(ConcurrentQueue<string> receivedMessages, StringBuilder buffer, Action<string> logger)
        {
            //logger("Reading serial...");

            var read = false;

            var availableCharacters = Pi.Serial.GetAvailableDataCount();
            while (availableCharacters > 0)
            {
                read = true;

                //logger($"Got {availableCharacters} characters...");

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

            //logger("Serial read!");

            return read;
        }

        internal static void HandleReceivedMessages(ConcurrentQueue<string> receivedMessages, IModuleConfiguration moduleConfiguration, Action<string> logger)
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

                    var isFailed = text.StartsWith("FAIL ", StringComparison.InvariantCultureIgnoreCase);
                    var isOk = text.StartsWith("OK ", StringComparison.InvariantCultureIgnoreCase);
                    var isPacket = !(isFailed || isOk);

                    var receivedMessage = new ReceivedMessage(text, DateTime.Now, isPacket ? (bool?)null : isFailed, isPacket);

                    context.ReceivedMessages.Add(receivedMessage);

                    context.SaveChanges();

                    if (isPacket)
                        arePacketsProcessed = true;

                    logger($"Processed message '{text}' to #{receivedMessage.ID}!"); // ~27ms, 22 - 34ms

                    if (isPacket || !_lastMessageSentID.HasValue)
                        continue;

                    var state = isOk ? SentMessageStateEnum.Completed : SentMessageStateEnum.Failed;

                    context.Database.ExecuteSqlCommand(Message.GenerateUpdate(context.GetTableName<Message>(),
                        new Dictionary<Expression<Func<Message, object>>, string>
                        {
                            {i => i.State, state.ToSql()},
                            {i => i.ResultMessageID, receivedMessage.ID.ToSql()},
                            {i => i.ResponseReceived, receivedMessage.Received.ToSql()}
                        },
                        new KeyValuePair<Expression<Func<Message, object>>, string>(i => i.ID, _lastMessageSentID.Value.ToSql())));

                    logger($"Updated message #{_lastMessageSentID.Value} with state '{state}'!"); // ~45ms

                    _lastMessageSentID = null;
                }
            }

            if (arePacketsProcessed && EngineProcessID.HasValue)
                Signal.Send(EngineProcessID.Value, SignalTypeEnum.User1);
        }

        private static void PollMessagesToSend(Queue<Message> messagesToSend, IModuleConfiguration moduleConfiguration, Action<string> logger) // ~32ms
        { // ~20ms
            logger("Polling messages to send...");

            using (var context = PiSensorNetDbContext.Connect(moduleConfiguration.ConnectionString))
            {
                var messages = context.Messages
                                      .Include(i => i.Function)
                                      .Include(i => i.Module)
                                      .AsNoTracking()
                                      .Where(i => i.State == SentMessageStateEnum.Queued)
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
            
            //logger($"Putting '{text}'..."); // ~0.5ms
            
            Pi.Serial.Put(text);

            logger("Put!"); // <1ms
            
            using (var context = PiSensorNetDbContext.Connect(moduleConfiguration.ConnectionString))
            {
                var sqls = new StringBuilder(200);
                var now = DateTime.Now;

                sqls.AppendLine(Message.GenerateUpdate(context.GetTableName<Message>(),
                    new Dictionary<Expression<Func<Message, object>>, string>
                    {
                        {i => i.State, SentMessageStateEnum.Sent.ToSql()},
                        {i => i.Sent, now.ToSql()},
                    },
                    new KeyValuePair<Expression<Func<Message, object>>, string>(i => i.ID, messageToSend.ID.ToSql())));

                //sqls.AppendLine(SentMessage.GenerateUpdate(context.GetTableName<SentMessage>(),
                //    new Dictionary<Expression<Func<SentMessage, object>>, string>
                //    {
                //        {i => i.Sent, now.ToSql()},
                //    },
                //    new KeyValuePair<Expression<Func<SentMessage, object>>, string>(i => i.MessageID, messageToSend.MessageID.ToSql())));

                context.Database.ExecuteSqlCommand(sqls.ToString());
            }
            
            logger("Message sent!"); // ~50ms

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
