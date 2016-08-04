using System;
using System.Linq;
using piSensorNet.DataModel.Enums;

namespace piSensorNet.Logic.Custom
{
    public struct FunctionHandlerResult
    {
        public PacketStateEnum PacketState { get; private set; }
        public bool ShouldHandlePacketsAgain { get; private set; }
        public bool NewMessagesAdded { get; private set; }

        public FunctionHandlerResult(PacketStateEnum packetState, bool newMessagesAdded = false, bool shouldHandlePacketsAgain = false)
        {
            PacketState = packetState;
            ShouldHandlePacketsAgain = shouldHandlePacketsAgain;
            NewMessagesAdded = newMessagesAdded;
        }

        public static implicit operator FunctionHandlerResult(PacketStateEnum state)
            => new FunctionHandlerResult(state);
    }
}