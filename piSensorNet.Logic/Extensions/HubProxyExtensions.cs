using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using piSensorNet.Logic;

namespace Microsoft.AspNet.SignalR.Client
{
    public static class MainHubEngineHubProxyExtensions
    {
        public static Task SafeInvoke(this IHubProxy proxy, Expression<Action<IMainHubEngine>> expression)
            => CustomHubProxyExtensions.SafeInvoke(proxy, expression);
    }
}
