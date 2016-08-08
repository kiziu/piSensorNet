using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using piSensorNet.DataModel.Context;

namespace piSensorNet.Web.SignalR
{
    public partial class MainHub : Hub
    {
        private static readonly string EngineQueryStringKey = Startup.Configuration["Settings:SignalREngineFlagName"];
        private static readonly string EngineQueryStringValue = true.ToString().ToLowerInvariant();

        private static string Now => DateTime.Now.ToString("O");

        public static string EngineClientConnectionID { get; private set; }

        public dynamic NonEngine => Clients.AllExcept(EngineClientConnectionID);
        public EngineProxy Engine => new EngineProxy(Clients.Client(EngineClientConnectionID), Context);

        private readonly Func<PiSensorNetDbContext> _contextFactory;
        public MainHub([NotNull] Func<PiSensorNetDbContext> contextFactory)
        {
            if (contextFactory == null) throw new ArgumentNullException(nameof(contextFactory));

            _contextFactory = contextFactory;
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            Console.WriteLine($"{Now}: Disconnected, ID {Context.ConnectionId}{(IsEngine() ? ", engine" : "")}.");

            return base.OnDisconnected(stopCalled);
        }

        public override Task OnConnected()
        {
            TryIdentifyEngine(Context);

            Console.WriteLine($"{Now}: Connected, ID: {Context.ConnectionId}{(IsEngine() ? ", engine": "")}, transport: {Context.QueryString["transport"]}.");

            return base.OnConnected();
        }

        public override Task OnReconnected()
        {
            TryIdentifyEngine(Context);

            Console.WriteLine($"{Now}: Reconnected, ID: {Context.ConnectionId}{(IsEngine() ? ", engine" : "")}, transport: {Context.QueryString["transport"]}.");

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
        private static bool TryIdentifyEngine(HubCallerContext context)
        {
            if (!String.Equals(EngineQueryStringValue, context.QueryString[EngineQueryStringKey]))
                return false;

            EngineClientConnectionID = context.ConnectionId;

            return true;
        }

        private void SendError(string message)
        {
            SendError(Context.ConnectionId, message);
        }

        private void SendError(string clientID, string message)
        {
            Clients.Client(clientID).error(message);
        }
    }
}
