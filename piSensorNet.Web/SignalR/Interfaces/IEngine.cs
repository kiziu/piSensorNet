using System;
using System.Linq;
using piSensorNet.Common.Enums;

namespace piSensorNet.Web.SignalR.Interfaces
{
    public interface IEngine
    {
        void SendMessage(string clientID, int? moduleID, FunctionTypeEnum functionType, string text = null);
        void SendQuery(string clientID, int? moduleID, FunctionTypeEnum functionType);
    }
}