using System;
using System.Linq;
using Microsoft.AspNet.SignalR.Hubs;
using piSensorNet.DataModel.Enums;

namespace piSensorNet.Web.SignalR
{
    public sealed class EngineProxy
    {
        private readonly dynamic _engine;
        private readonly HubCallerContext _context;

        public EngineProxy(dynamic engine, HubCallerContext context)
        {
            _engine = engine;
            _context = context;
        }

        public void SendMessage(int? moduleID, FunctionTypeEnum functionType, string text = null)
        {
            _engine.sendMessage(_context.ConnectionId, moduleID, functionType, text);
        }

        public void SendQuery(int? moduleID, FunctionTypeEnum functionType)
        {
            _engine.sendQuery(_context.ConnectionId, moduleID, functionType);
        }
    }
}