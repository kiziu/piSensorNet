using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using piSensorNet.Common.Enums;
using piSensorNet.Common.Extensions;
using piSensorNet.Engine;
using piSensorNet.Radio.NrfNet;
using piSensorNet.WiringPi.Enums;

namespace piSensorNet.ModuleTester
{
    public class TesterMain
    {
        private const int Timeout = 200;
        private const bool MeasureTime = true;

        private static readonly Dictionary<int, DateTime> MessageQueuedTimes = new Dictionary<int, DateTime>(10);
        private static readonly Dictionary<int, DateTime> MessageSentTimes = new Dictionary<int, DateTime>(10);

        public static int Main(string[] args)
        {
            var id = 1;
            var readRetransmissionsCount = args.Any(i => i.Equals("read", StringComparison.InvariantCultureIgnoreCase));

            using (var module = new Processor(PinNumberEnum.Gpio0, SpiChannelEnum.One, (Address)"kiziu", (Address)"kizie", 13,
                readRetransmissionsCount: readRetransmissionsCount))
            {
                module.Received += (sender, eventArgs) =>
                {
                    string timeTakenMessage;

                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (MeasureTime)
                    {
                        var messageID = eventArgs.Message.Split('#')[0].FromBase36();
                        var timeTaken = eventArgs.LastPacketReceived - MessageSentTimes[messageID];

                        timeTakenMessage = $" Roundtrip took {timeTaken.TotalMilliseconds}ms.";
                    }

                    Console.WriteLine($"<-- {DateTime.UtcNow.ToFullTimeString()}: Received from {eventArgs.Sender} ({eventArgs.ChunksCount} chunk(s)): {eventArgs.Message} = {eventArgs.TimeTaken.TotalMilliseconds:N2}ms.{timeTakenMessage}");
                };

                module.Sent += (sender, eventArgs) =>
                {
                    string timeTakenMessage;

                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (MeasureTime)
                    {
                        MessageSentTimes.Add(eventArgs.ID, eventArgs.FirstPacketSent);
                        var timeTaken = eventArgs.FirstPacketSent - MessageQueuedTimes[eventArgs.ID];

                        timeTakenMessage = $" Dequeueing took {timeTaken.TotalMilliseconds}ms.";
                    }

                    Console.WriteLine($"--> {DateTime.UtcNow.ToFullTimeString()}: Sent message #{eventArgs.ID} ({eventArgs.ChunksCount} chunk(s){(readRetransmissionsCount ? $"; {eventArgs.RetransmissionsCounts.Join(",")}" : String.Empty)}) = {eventArgs.TimeTaken.TotalMilliseconds:N2}ms.{timeTakenMessage}");
                };

                module.Failed += (sender, eventArgs) =>
                    Console.WriteLine($"!!! {DateTime.UtcNow.ToFullTimeString()}: Failed message #{eventArgs.ID}, sequence {eventArgs.Sequence} on packet #{eventArgs.Current} of {eventArgs.Total}.");

                module.Error += (sender, eventArgs) =>
                    Console.WriteLine($"!!! {DateTime.UtcNow.ToFullTimeString()}: Error: {eventArgs.Exception.Message}.");

                var functions = typeof(FunctionTypeEnum).GetEnumValues()
                                                        .Cast<FunctionTypeEnum>()
                                                        .Where(i => i != FunctionTypeEnum.Unknown)
                                                        .Select(i => new
                                                                     {
                                                                         Type = i,
                                                                         Name = i.ToFunctionName(),
                                                                         Option = i.ToFunctionName()
                                                                                   .Split('_')
                                                                                   .Select(ii => ii[0])
                                                                                   .Concat()
                                                                     })
                                                        .ToList();

                module.Start(true);

                var loop = true;
				Address recipientAddress = null;
                do
                {
                    Console.WriteLine();
                    Console.WriteLine("Choose option: ");
                    Console.WriteLine("\tbi - broadcast 'identify'");
                    Console.WriteLine("\tmsg - send custom message (auto-packeting)");
                    functions.ForEach(f => Console.WriteLine($"\t\t{f.Option} - send '{f.Name}'"));
                    Console.WriteLine("\tss - show status");
                    Console.WriteLine("\tclr - clear");
                    Console.WriteLine("\tq - quit");
                    Console.WriteLine();

                    SELECTION:
                    Console.Write("Enter selection: ");

                    var selection = Regex.Replace(Console.ReadLine() ?? String.Empty, "[^a-z]", String.Empty, RegexOptions.IgnoreCase)
                                         .ToLowerInvariant();

                    if (String.IsNullOrEmpty(selection))
                        goto SELECTION;

                    var function = functions.Where(i => i.Option == selection).SingleOrDefault();
                    if (function != null)
                    {
                        id = SendMessage(module, id, null, ref recipientAddress, function.Name, false);
                        continue;
                    }

                    // ReSharper disable once TooWideLocalVariableScope
                    string message;
                    switch (selection)
                    {
                        case "clr":
                            Console.Clear();
                            break;

                        case "ss":
                            var status = module.GetStatus();

                            Console.WriteLine();
                            Console.WriteLine("Status:");
                            foreach (var property in status.GetType().GetProperties())
                                Console.WriteLine($"- {property.Name}: {Format(property.GetValue(status))}");
                            break;

                        case "bi":
                            id = SendMessage(module, id, module.BroadcastAddress, ref recipientAddress, "identify", true);
                            break;

                        case "msg":
                            bool broadcast;
                            if (!RequestRecipientAddress(ref recipientAddress)) continue;
                            if (!Ask("Send as broadcast?", out broadcast)) continue;
                            if (!RequestMessage(out message)) continue;

                            id = SendMessage(module, id, null, ref recipientAddress, message, broadcast);
                            break;

                        case "q":
                            Console.WriteLine();
                            Console.WriteLine("Quitting...");
                            loop = false;
                            break;

                        default:
                            Console.WriteLine();
                            Console.WriteLine($"!!! {DateTime.UtcNow.ToFullTimeString()}: ERROR: Unknown option '{selection}'.");
                            continue;
                    }
                } while (loop);
            }

            return 0;
        }

        private static string Format(object obj)
        {
            var dictionary = obj as IDictionary;
            if (dictionary != null)
                return "{" + dictionary.Keys.Cast<object>().Select(i => $"[{i}, {dictionary[i]}]").Join(", ") + "}";

            var collection = obj as ICollection;
            if (collection != null)
                return "{" + collection.Cast<object>().Join(", ") + "}";

            return obj.ToString();
        }

        private static int SendMessage(Processor module, int id, Address address, ref Address recipientAddress, string message, bool isBroadcast)
        {
            if (address == null && !RequestRecipientAddress(ref recipientAddress))
                return id;

            address = address ?? recipientAddress;
            recipientAddress = address;

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (MeasureTime)
            {
                message = id.ToBase36() + "#" + message;
                MessageQueuedTimes.Add(id, DateTime.UtcNow);
            }

            var messageCount = module.EnqueueForSending(id, address, message, !isBroadcast);

            Console.WriteLine();
            Console.WriteLine($"~~~ {DateTime.UtcNow.ToFullTimeString()}: Enqueued{(isBroadcast ? " broadcasted" : String.Empty)} '{message}' #{id} split into {messageCount} packet(s) to {address}.");
            ++id;

            Thread.Sleep(Timeout);

            return id;
        }

        private static bool RequestRecipientAddress(ref Address recipientAddress)
        {
            RequestRecipientAddress:

            var reuseAddressMessage = recipientAddress != null ? $", ENTER to reuse '{recipientAddress.Readable}'" : String.Empty;

            Console.Write($"Enter recipient (or q to cancel{reuseAddressMessage}): ");
			var left = Console.CursorLeft;
			
            var recipientString = (Console.ReadLine() ?? String.Empty).Trim();
            if (recipientString == "q")
                return false;

            if (string.IsNullOrEmpty(recipientString) && recipientAddress != null)
            {
				--Console.CursorTop;
				Console.CursorLeft = left;
				
                Console.WriteLine(recipientAddress.Readable);
				
                return true;
            }

            if (recipientString.Length != 5)
            {
                Console.WriteLine();
                Console.WriteLine($"ERROR: Wrong recipient '{recipientString}'.");
                Console.WriteLine();

                goto RequestRecipientAddress;
            }

            recipientAddress = (Address)recipientString;

            return true;
        }

        private static bool RequestMessage(out string message)
        {
            RequestMessage:

            Console.Write("Enter message (or q to cancel): ");
            message = (Console.ReadLine() ?? String.Empty).Trim();
            if (message == "q")
            {
                message = null;
                return false;
            }

            if (string.IsNullOrEmpty(message))
            {
                Console.WriteLine();
                Console.WriteLine("ERROR: Message cannot be empty.");
                Console.WriteLine();

                goto RequestMessage;
            }

            return true;
        }

        private static bool Ask(string question, out bool choice)
        {
            while (true)
            {
                Console.Write(question + " [y/n/q]: ");
                var answer = char.ToLowerInvariant(Console.ReadKey().KeyChar);
                Console.WriteLine();

                switch (answer)
                {
                    case 'y':
                        choice = true;

                        return true;

                    case 'n':
                        choice = false;

                        return true;

                    case 'q':
                        choice = false;

                        return false;

                    default:
                        continue;
                }
            }
        }
    }
}