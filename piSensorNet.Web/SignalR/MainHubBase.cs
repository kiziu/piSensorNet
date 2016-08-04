using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using piSensorNet.Logic.Services;

namespace piSensorNet.Web.SignalR
{
    public partial class MainHub : Hub
    {
        private static readonly string EngineQueryStringKey = Startup.Configuration["Settings:SignalREngineFlagName"];
        private static readonly string EngineQueryStringValue = true.ToString().ToLowerInvariant();
        
        public static string EngineClientConnectionID { get; private set; }

        public dynamic NonEngine => Clients.AllExcept(EngineClientConnectionID);
        public EngineProxy Engine => new EngineProxy(Clients.Client(EngineClientConnectionID), Context);

        private readonly ModulesService _modulesService;

        private static string Now => DateTime.Now.ToString("O");

        public MainHub(ModulesService modulesService)
        {
            if (modulesService == null) throw new ArgumentNullException(nameof(modulesService));

            _modulesService = modulesService;
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
