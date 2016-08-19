using System;
using System.Linq;
using System.Net;
using JetBrains.Annotations;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Filters;

namespace piSensorNet.Web.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class LocalOnlyAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization([NotNull] AuthorizationContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            var referer = request.Headers["Referer"];
            var host = request.Host.ToString();
            if (!String.IsNullOrEmpty(referer))
            {
                var refererUri = new Uri(referer);
                var refererHost = refererUri.Host + ":" + refererUri.Port;

                if (String.Equals(host, refererHost))
                    return;
            }

            filterContext.Result = new HttpStatusCodeResult((int)HttpStatusCode.Forbidden);
        }
    }
}