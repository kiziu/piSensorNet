//using System;
//using System.Collections.Generic;
//using System.Data.Entity;
//using System.Linq;
//using Microsoft.AspNet.SignalR.Client;
//using piSensorNet.Common.Enums;
//using piSensorNet.DataModel.Context;
//using piSensorNet.DataModel.Enums;
//using piSensorNet.Engine;
//using piSensorNet.Logic.FunctionHandlers.Base;
//using Xunit;

//using static piSensorNet.Tests.Common;

//namespace piSensorNet.Tests.Engine
//{
//    public class Main : TestClassBase
//    {
//        // ReSharper disable NotAccessedField.Local
//        private IReadOnlyDictionary<string, int> Modules;
//        private IReadOnlyDictionary<int, string> InverseModules;
//        private readonly IReadOnlyDictionary<int, IFunctionHandler> FunctionHandlers;
//        private readonly IReadOnlyDictionary<string, IQueryableFunctionHandler> QueryableFunctionHandlers;
//        private readonly IReadOnlyDictionary<FunctionTypeEnum, KeyValuePair<int, string>> Functions;
//        private readonly IReadOnlyDictionary<string, int> InverseFunctions;
//        // ReSharper enable NotAccessedField.Local

//        private readonly IHubProxy HubProxy;

//        protected override bool LogQueries { get; } = false;
//        protected override bool InTransaction { get; } = true;
//        private static readonly Action<string> Logger = EmptyLogger;

//        public Main()
//        {
//            InternalCacheModules(Context);

//            var f = EngineMain.CacheFunctions(Context);
//            Functions = f.Item1;
//            InverseFunctions = f.Item2;

//            var fh = EngineMain.CacheFunctionHandlers(Functions);
//            FunctionHandlers = fh.Item1;
//            QueryableFunctionHandlers = fh.Item2;

//            HubProxy = new ConsoleHubProxy(!true);
//        }

//        private Tuple<IReadOnlyDictionary<string, int>, IReadOnlyDictionary<int, string>> InternalCacheModules(PiSensorNetDbContext context)
//        {
//            var m = EngineMain.CacheModuleAddresses(context);

//            Modules = m.Item1;
//            InverseModules = m.Item2;

//            return m;
//        }

//        //[Theory]
//        //[InlineData('7', 1, 2, 3, "test")]
//        //public void GivenOneReceivedMessage_WhenPolledFor_ThenPartialPacketCreated(
//        //    char address, byte number, byte current, byte total, string text)
//        //{
//        //    var receivedMessage = new ReceivedMessage(TestsConfiguration.MessagePattern.AsFormatFor(address, number, current, total, text), DateTime.Now, null, true);

//        //    Context.ReceivedMessages.Add(receivedMessage);
//        //    Context.SaveChanges();

//        //    EngineMain.PollReceivedMessagesAndCreatePartialPackets(Context, TestsConfiguration, Logger);

//        //    Assert.True(Context.ReceivedMessages.AsNoTracking().Where(i => i.ID == receivedMessage.ID).Single().HasPartialPacket);

//        //    var partialPacket = Context.PartialPackets
//        //                               .AsNoTracking()
//        //                               .Where(i => i.ReceivedMessageID == receivedMessage.ID)
//        //                               .SingleOrDefault();

//        //    Assert.NotNull(partialPacket);

//        //    Assert.Equal(receivedMessage.ID, partialPacket.ReceivedMessageID);
//        //    Assert.Equal(TestsConfiguration.AddressPattern.AsFormatFor(address), partialPacket.Address);
//        //    Assert.Equal(text, partialPacket.Message);
//        //    Assert.Equal(current, partialPacket.Current);
//        //    Assert.Equal(total, partialPacket.Total);
//        //    Assert.Equal(number, partialPacket.Number);
//        //    Assert.Equal(receivedMessage.Received.TruncateMilliseconds(3), partialPacket.Received);
//        //}

//        //[Theory]
//        //[InlineData("FAIL Something went wrong", true)]
//        //[InlineData("OK 1/1", false)]
//        //public void GivenOneNonPacketReceivedMessage_WhenPolledFor_ThenNoPartialPacketCreated(
//        //    string text, bool isFailed)
//        //{
//        //    var receivedMessage = new ReceivedMessage(text, DateTime.Now, isFailed, false);

//        //    Context.ReceivedMessages.Add(receivedMessage);
//        //    Context.SaveChanges();

//        //    EngineMain.PollReceivedMessagesAndCreatePartialPackets(Context, TestsConfiguration, Logger);

//        //    Assert.Null(Context.PartialPackets.AsNoTracking().Where(i => i.ReceivedMessageID == receivedMessage.ID).SingleOrDefault());
//        //}

//        //[Theory]
//        //[InlineData('7', 1, 2, 3, "test1",
//        //            '8', 2, 3, 4, "test2")]
//        //[InlineData('9', 1, 1, 2, "ow_ds18b20_temperature_period",
//        //            '9', 1, 2, 2, "ical=50")]
//        //public void GivenAtLeastTwoReceivedMessages_WhenPolledFor_ThenPartialPacketsCreated(
//        //    char address1, byte number1, byte current1, byte total1, string text1,
//        //    char address2, byte number2, byte current2, byte total2, string text2)
//        //{
//        //    var receivedMessage1 = new ReceivedMessage(TestsConfiguration.MessagePattern.AsFormatFor(address1, number1, current1, total1, text1), DateTime.Now, null, true);
//        //    var receivedMessage2 = new ReceivedMessage(TestsConfiguration.MessagePattern.AsFormatFor(address2, number2, current2, total2, text2), DateTime.Now, null, true);

//        //    Context.ReceivedMessages.Add(receivedMessage1);
//        //    Context.ReceivedMessages.Add(receivedMessage2);
//        //    Context.SaveChanges();

//        //    EngineMain.PollReceivedMessagesAndCreatePartialPackets(Context, TestsConfiguration, Logger);

//        //    AssertPartialPacket(receivedMessage1, address1, number1, current1, total1, text1);
//        //    AssertPartialPacket(receivedMessage2, address2, number2, current2, total2, text2);
//        //}

//        //[SuppressMessage("ReSharper", "UnusedParameter.Local")]
//        //private void AssertPartialPacket(ReceivedMessage receivedMessage, char address, byte number, byte current, byte total, string text)
//        //{
//        //    Assert.True(Context.ReceivedMessages.AsNoTracking().Where(i => i.ID == receivedMessage.ID).Single().HasPartialPacket);

//        //    var partialPacket = Context.PartialPackets
//        //                               .AsNoTracking()
//        //                               .Where(i => i.ReceivedMessageID == receivedMessage.ID)
//        //                               .SingleOrDefault();

//        //    Assert.NotNull(partialPacket);

//        //    Assert.Equal(receivedMessage.ID, partialPacket.ReceivedMessageID);
//        //    Assert.Equal(TestsConfiguration.AddressPattern.AsFormatFor(address), partialPacket.Address);
//        //    Assert.Equal(text, partialPacket.Message);
//        //    Assert.Equal(current, partialPacket.Current);
//        //    Assert.Equal(total, partialPacket.Total);
//        //    Assert.Equal(number, partialPacket.Number);
//        //    Assert.Equal(receivedMessage.Received.TruncateMilliseconds(3), partialPacket.Received);
//        //}

//        //[Theory]
//        //[InlineData('7', 1, FunctionTypeEnum.OwDS18B20Temperature, "ow_ds18b20_temperature=2854280E02000070|25.75;28AC5F2600008030|25.75;")]
//        //[InlineData('7', 2, FunctionTypeEnum.OwDS18B20TemperaturePeriodical, "ow_ds18b20_temperature_periodical=50")]
//        //[InlineData('7', 3, FunctionTypeEnum.Unknown, "dummy_function=50")]
//        //public void GivenPartialPacketsForOnePacket_WhenMerged_ThenPacketCreated(
//        //    char address, byte number, FunctionTypeEnum functionType, string text)
//        //{
//        //    var received = DateTime.Now;
//        //    var fullAddress = TestsConfiguration.AddressPattern.AsFormatFor(address);

//        //    var chunks = text.Chunkify(28).ToList();
//        //    for (byte i = 0, total = (byte)chunks.Count; i < total; ++i)
//        //    {
//        //        var receivedMessage = new ReceivedMessage(TestsConfiguration.MessagePattern.AsFormatFor(address, number, i + 1, total, chunks[i]), DateTime.Now, null, true);

//        //        Context.ReceivedMessages.Add(receivedMessage);

//        //        var partialPacket = new PartialPacket(receivedMessage, fullAddress, number, (byte)(i + 1), total, chunks[i], receivedMessage.Received);

//        //        Context.PartialPackets.Add(partialPacket);

//        //        received = receivedMessage.Received;
//        //    }

//        //    Context.SaveChanges();

//        //    EngineMain.MergePackets(Context, TestsConfiguration, EngineMain.DefaultFunctions, Functions, InverseFunctions, Modules, HubProxy, InternalCacheModules, Logger);

//        //    var packet = Context.Packets
//        //                        .AsNoTracking()
//        //                        .Include(i => i.Module)
//        //                        .Include(i => i.Function)
//        //                        .Where(i => i.Number == number)
//        //                        .Where(i => i.Module.Address == fullAddress)
//        //                        .SingleOrDefault();

//        //    Assert.NotNull(packet);

//        //    Assert.Equal(functionType, packet.Function?.FunctionType ?? FunctionTypeEnum.Unknown);
//        //    Assert.Equal(functionType == FunctionTypeEnum.Unknown ? text : text.SubstringAfter(TestsConfiguration.FunctionResultNameDelimiter), packet.Text);
//        //    Assert.Equal(received.TruncateMilliseconds(3), packet.Received);
//        //}

//        //[Theory]
//        //[InlineData('7', 1, FunctionTypeEnum.OwDS18B20Temperature, "ow_ds18b20_temperature=2854280E02000070|25.75;28AC5F2600008030|25.75;",
//        //            '7', 2, FunctionTypeEnum.OwDS18B20TemperaturePeriodical, "ow_ds18b20_temperature_periodical=50")]
//        //public void GivenPartialPacketsForAtLeastOnePacket_WhenMerged_ThenPacketCreated(
//        //    char address1, byte number1, FunctionTypeEnum functionType1, string text1,
//        //    char address2, byte number2, FunctionTypeEnum functionType2, string text2)
//        //{
//        //    var received1 = DateTime.Now;
//        //    var fullAddress1 = TestsConfiguration.AddressPattern.AsFormatFor(address1);

//        //    var chunks1 = text1.Chunkify(28).ToList();
//        //    for (byte i = 0, total = (byte)chunks1.Count; i < total; ++i)
//        //    {
//        //        var receivedMessage = new ReceivedMessage(TestsConfiguration.MessagePattern.AsFormatFor(address1, number1, i + 1, total, chunks1[i]), DateTime.Now, null, true);

//        //        Context.ReceivedMessages.Add(receivedMessage);

//        //        var partialPacket = new PartialPacket(receivedMessage, fullAddress1, number1, (byte)(i + 1), total, chunks1[i], receivedMessage.Received);

//        //        Context.PartialPackets.Add(partialPacket);

//        //        received1 = receivedMessage.Received;
//        //    }

//        //    var received2 = DateTime.Now;
//        //    var fullAddress2 = TestsConfiguration.AddressPattern.AsFormatFor(address2);

//        //    var chunks2 = text2.Chunkify(28).ToList();
//        //    for (byte i = 0, total = (byte)chunks2.Count; i < total; ++i)
//        //    {
//        //        var receivedMessage = new ReceivedMessage(TestsConfiguration.MessagePattern.AsFormatFor(address2, number2, i + 1, total, chunks2[i]), DateTime.Now, null, true);

//        //        Context.ReceivedMessages.Add(receivedMessage);

//        //        var partialPacket = new PartialPacket(receivedMessage, fullAddress2, number2, (byte)(i + 1), total, chunks2[i], receivedMessage.Received);

//        //        Context.PartialPackets.Add(partialPacket);

//        //        received2 = receivedMessage.Received;
//        //    }

//        //    Context.SaveChanges();

//        //    EngineMain.MergePackets(Context, TestsConfiguration, EngineMain.DefaultFunctions, Functions, InverseFunctions, Modules, HubProxy, InternalCacheModules, Logger);

//        //    AssertPacket(number1, functionType1, text1, fullAddress1, received1);
//        //    AssertPacket(number2, functionType2, text2, fullAddress2, received2);
//        //}

//        //[SuppressMessage("ReSharper", "UnusedParameter.Local")]
//        //private void AssertPacket(byte number, FunctionTypeEnum functionType, string text, string fullAddress, DateTime received)
//        //{
//        //    var packet = Context.Packets
//        //                        .AsNoTracking()
//        //                        .Include(i => i.Module)
//        //                        .Include(i => i.Function)
//        //                        .Where(i => i.Number == number)
//        //                        .Where(i => i.Module.Address == fullAddress)
//        //                        .SingleOrDefault();

//        //    Assert.NotNull(packet);

//        //    Assert.Equal(functionType, packet.Function.FunctionType);
//        //    Assert.Equal(text.SubstringAfter(TestsConfiguration.FunctionResultNameDelimiter), packet.Text);
//        //    Assert.Equal(received.TruncateMilliseconds(3), packet.Received);
//        //}

//        //[Theory]
//        //[InlineData(null, FunctionTypeEnum.Identify)]
//        //public void GivenHubMessage_WhenBroadcasted_ThenHandled(int? moduleID, FunctionTypeEnum functionType)
//        //{
//        //    var builder = new StringBuilder();

//        //    EngineMain.HandleMessage("dummyClientID", moduleID, functionType, false, null, InverseModules, TestsConfiguration, Functions, builder, HubProxy, Logger);


//        //}
//    }
//}