using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using piSensorNet.Common;
using piSensorNet.Common.Custom;
using piSensorNet.Common.Custom.Interfaces;
using piSensorNet.Common.Enums;
using piSensorNet.Common.Extensions;
using piSensorNet.Common.System;
using piSensorNet.DataModel.Context;
using piSensorNet.DataModel.Entities;
using piSensorNet.WiringPi.Enums;
using piSensorNet.WiringPi;
using static piSensorNet.Common.Helpers.LoggingHelper;

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

        private static IpiSensorNetConfiguration Configuration { get; } = ReadOnlyConfiguration.Load<IpiSensorNetConfiguration>("config.json");

        private static int? _lastMessageSentID;

        private static readonly ConcurrentQueue<string> ReceivedMessages = new ConcurrentQueue<string>();
        private static readonly StringBuilder Buffer = new StringBuilder(1024);
        private static readonly Queue<Message> MessagesToSend = new Queue<Message>(10);
        private static readonly StringBuilder MessageBuilder = new StringBuilder(200);

        private static volatile int _readSerial;
        private static volatile bool _doQuit;
        private static volatile bool _pollMessagesToSend;

        public static int Main(string[] args)
        {
            ToConsole("Main: Initializing Serial Monitor..");

            var toDispose = new DisposalQueue();
            var engineProcessID = FindSerialProcessID(Configuration, ToConsole);

            PiSensorNetDbContext.Initialize(Configuration.ConnectionString);

            //PiSensorNetDbContext.Logger = Console.Write;

            ToConsole("Main: Context initialized!");

            toDispose += Signal.Handle(SignalHandlers);

            Functionalities.Serial.Open();

            Functionalities.Pins.Setup(BroadcomPinNumberEnum.Gpio18, PinModeEnum.Input, PullUpModeEnum.Up);

            toDispose += Functionalities.Interrupts.SetupPolled(BroadcomPinNumberEnum.Gpio18, InterruptModeEnum.FallingEdge, SerialInterruptHandler);

            ToConsole("Main: Started!");

            while (!_doQuit)
            {
                WaitHandle.WaitOne(_readSerial == 0 ? -1 : 3);

                if (_readSerial > 0)
                {
                    --_readSerial;
                    if (ReadSerial(ReceivedMessages, Buffer, ToConsole))
                    {
                        ++_readSerial;
                        continue;
                    }

                    if (_readSerial > 0)
                        continue;
                }

                HandleReceivedMessages(engineProcessID, ReceivedMessages, Configuration, ToConsole);

                if (_pollMessagesToSend)
                {
                    _pollMessagesToSend = false;
                    PollMessagesToSend(MessagesToSend, Configuration, ToConsole);
                }

                _lastMessageSentID = SendMessage(MessagesToSend, _lastMessageSentID, Configuration, MessageBuilder, ToConsole);
            }

            ToConsole("Main: Stopping...");
            
            toDispose.Dispose();

            Functionalities.Interrupts.Remove(BroadcomPinNumberEnum.Gpio18);

            Functionalities.Serial.Flush();
            Functionalities.Serial.Close();

            ToConsole("Main: Stopped!");

            return 0;
        }
        
        private static int? FindSerialProcessID(IReadOnlyConfiguration configuration, Action<string> logger)
        {
            if (Constants.IsWindows)
                throw new Exception("Linux only!");

            var nameFragment = configuration["Settings:EngineProcessNameFragment"];
            
            var pid = Processess.FindByFragment(nameFragment, StringComparison.InvariantCulture, "sudo");
            if (!pid.HasValue)
            {
                logger($"FindSerialProcessID: ERROR: Engine process with name fragment '{nameFragment}' not found!");
                Environment.Exit(-1);
            }

            logger($"FindSerialProcessID: Found process #{pid.Value} with name fragment '{nameFragment}!");

            return pid.Value;
        }

        private static bool ReadSerial(ConcurrentQueue<string> receivedMessages, StringBuilder buffer, Action<string> logger)
        {
            var read = false;

            var availableCharacters = Functionalities.Serial.GetAvailableDataCount();
            while (availableCharacters > 0)
            {
                read = true;

                for (var i = 0; i < availableCharacters; ++i)
                {
                    var readCharacter = Functionalities.Serial.Get();
                    if (readCharacter == Functionalities.Serial.Terminator)
                    {
                        var item = buffer.ToString();

                        logger($"ReadSerial: Enqueuing item: '{item}'!");

                        receivedMessages.Enqueue(item);
                        buffer.Clear();

                        continue;
                    }

                    if (readCharacter == '\r')
                        continue;

                    buffer.Append(readCharacter);
                }

                availableCharacters = Functionalities.Serial.GetAvailableDataCount();
            }

            return read;
        }

        internal static void HandleReceivedMessages(int? engineProcessID, ConcurrentQueue<string> receivedMessages, IpiSensorNetConfiguration moduleConfiguration, Action<string> logger)
        {
            if (receivedMessages.Count == 0)
                return;

            logger("HandleReceivedMessages: Start...");

            var arePacketsProcessed = false;
            using (var context = PiSensorNetDbContext.Connect(moduleConfiguration.ConnectionString))
            {
                while (receivedMessages.Count > 0)
                {
                    logger("HandleReceivedMessages: Dequeing...");

                    string text;
                    if (!receivedMessages.TryDequeue(out text))
                        continue;

                    logger($"HandleReceivedMessages: Processing '{text}'...");

                    var isFailed = text.StartsWith("FAIL ", StringComparison.InvariantCultureIgnoreCase);
                    var isOk = text.StartsWith("OK ", StringComparison.InvariantCultureIgnoreCase);

                    if (isFailed || isOk)
                    {
                        if (!_lastMessageSentID.HasValue)
                        {
                            logger($"HandleReceivedMessages: ERROR: Message '{text} was not handled due to lack of {nameof(_lastMessageSentID)}!");
                            continue;
                        }

                        var state = isOk ? MessageStateEnum.Completed : MessageStateEnum.Failed;
                        var error = isFailed ? text.Substring("FAIL ".Length) : null;
                        
                        //context.EnqueueRaw(Message.GenerateUpdate(context,
                        //    new Dictionary<Expression<Func<Message, object>>, string>
                        //    {
                        //        {i => i.State, state.ToSql()},
                        //        {i => i.ResponseReceived, DateTime.Now.ToSql()},
                        //        {i => i.Error, error.ToSql()},
                        //    },
                        //    new Tuple<Expression<Func<Message, object>>, string, string>(i => i.ID, "=", _lastMessageSentID.Value.ToSql())));

                        context.EnqueueUpdate<Message>(
                            i => i.State == state && i.ResponseReceived == DateTime.Now && i.Error == error,
                            i => i.ID == _lastMessageSentID.Value);

                        logger($"HandleReceivedMessages: Updated message #{_lastMessageSentID.Value} to '{state}'!");
                        
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

                        logger("HandleReceivedMessages: Processed message to new partial packet!");
                    }
                }

                context.SaveChanges();

                logger("HandleReceivedMessages: Done!"); // ~64ms, 4 messagess
            }

            if (arePacketsProcessed && engineProcessID.HasValue)
                Signal.Send(engineProcessID.Value, SignalTypeEnum.User1);
        }

        private static void PollMessagesToSend(Queue<Message> messagesToSend, IpiSensorNetConfiguration moduleConfiguration, Action<string> logger)
        {
            logger("PollMessagesToSend: Start...");

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

                logger($"PollMessagesToSend: Enqueued {messages.Count} messages(s)!"); // ~25ms, 1 message
            }
        }

        private static string AssembleMessage(StringBuilder builder, Message messageToSend, IpiSensorNetConfiguration moduleConfiguration)
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

        private static int? SendMessage(Queue<Message> messagesToSend, int? lastMessageSentID, IpiSensorNetConfiguration moduleConfiguration, StringBuilder messageBuilder, Action<string> logger)
        {
            if (messagesToSend.Count == 0 || lastMessageSentID.HasValue)
                return lastMessageSentID;

            logger("SendMessage: Start...");

            var messageToSend = messagesToSend.Dequeue();
            var text = AssembleMessage(messageBuilder, messageToSend, moduleConfiguration);

            logger($"SendMessage: Putting '{text}'..."); // <0.5ms

            Functionalities.Serial.Put(text);
            
            using (var context = PiSensorNetDbContext.Connect(moduleConfiguration.ConnectionString))
            {
                context.EnqueueUpdate<Message>(
                    i => i.State == MessageStateEnum.Sent && i.Sent == DateTime.Now,
                    i => i.ID == messageToSend.ID);

                context.ExecuteRaw();
            }

            logger("SendMessage: Message sent!"); // ~22ms

            return messageToSend.ID;
        }

        #region Handlers

        private static void SerialInterruptHandler()
        {
            ToConsole("Received Serial Interrupt!");

            ++_readSerial;
            WaitHandle.Set();
        }

        private static void QuitSignalHandler(SignalTypeEnum signalType)
        {
            ToConsole($"Received quit signal as '{signalType}'!");

            _doQuit = true;
            WaitHandle.Set();
        }

        private static void NeMessageToSendSignalHandler(SignalTypeEnum signalType)
        {
            ToConsole("Received new message signal!");

            _pollMessagesToSend = true;
            WaitHandle.Set();
        }

        #endregion

    }
}
