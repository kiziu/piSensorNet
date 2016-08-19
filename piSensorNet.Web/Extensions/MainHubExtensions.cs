using System;
using System.Linq;
using Microsoft.AspNet.SignalR;
using piSensorNet.Web.SignalR;

namespace piSensorNet.Web.Extensions
{
    public static class MainHubExtensions
    {
        public static dynamic Engine(this IHubContext<MainHub> hubContext) 
            => hubContext.Clients.Client(MainHub.EngineClientConnectionID);

        public static dynamic NonEngine(this IHubContext<MainHub> hubContext) 
            => hubContext.Clients.AllExcept(MainHub.EngineClientConnectionID);
    }
}