using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using piSensorNet.Common.Extensions;
using piSensorNet.Web.SignalR.Interfaces;

using static piSensorNet.Common.Helpers.LoggingHelper;

namespace piSensorNet.Web.SignalR
{
    public partial class MainHub : Hub<IUser>
    {
        private static readonly string EngineQueryStringKey = Startup.Configuration["Settings:SignalREngineFlagName"];
        private static readonly string EngineQueryStringValue = true.ToString().ToLowerInvariant();

        public static string EngineClientConnectionID { get; private set; }

        [NotNull]
        public IClient NonEngine => Clients.AllExcept(EngineClientConnectionID);

        [CanBeNull]
        public IEngine Engine => EngineClientConnectionID?.For(Clients.Client);

        public override Task OnDisconnected(bool stopCalled)
        {
            TryIdentifyEngine(Context, true);

            ToConsole($"Disconnected, ID {Context.ConnectionId}{(IsEngine() ? ", engine" : "")}");

            return base.OnDisconnected(stopCalled);
        }

        public override Task OnConnected()
        {
            TryIdentifyEngine(Context);

            ToConsole($"Connected, ID: {Context.ConnectionId}{(IsEngine() ? ", engine" : "")}, transport: {Context.QueryString["transport"]}, ip: {Context.Request.HttpContext.Connection.RemoteIpAddress}");

            return base.OnConnected();
        }

        public override Task OnReconnected()
        {
            TryIdentifyEngine(Context);

            ToConsole($"Reconnected, ID: {Context.ConnectionId}{(IsEngine() ? ", engine" : "")}, transport: {Context.QueryString["transport"]}, ip: {Context.Request.HttpContext.Connection.RemoteIpAddress}");

            return base.OnReconnected();
        }

        protected bool IsEngine()
        {
            return IsEngine(Context);
        }

        public static bool IsEngine(HubCallerContext context)
        {
            return String.Equals(EngineClientConnectionID, context.ConnectionId);
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private static bool TryIdentifyEngine(HubCallerContext context, bool disconnect = false)
        {
            if (!String.Equals(EngineQueryStringValue, context.QueryString[EngineQueryStringKey]))
                return false;

            EngineClientConnectionID = disconnect ? null : context.ConnectionId;

            return true;
        }

        private void SendError(string message)
        {
            SendError(Context.ConnectionId, message);
        }

        private void SendError(string clientID, string message)
        {
            if (clientID == null)
                Clients.All.OnError(message);

            Clients.Client(clientID).OnError(message);
        }
    }
}