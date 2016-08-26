using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using piSensorNet.Common.Extensions;

namespace piSensorNet.Web.Extensions
{
    public static class UrlHelperExtensions
    {
        [NotNull]
        public static string Action([NotNull] this IUrlHelper helper, [AspMvcAction] string action, [AspMvcController] string controller, [NotNull] string area, [CanBeNull] object routeValues = null) =>
            helper.Action(action, controller,
                new RouteValueDictionary(routeValues).AddOrReplace("Area", area),
                null, null, null);
    }
}