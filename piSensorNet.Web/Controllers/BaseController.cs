using System;
using System.Linq;
using Microsoft.AspNet.Mvc;

namespace piSensorNet.Web.Controllers
{
    public abstract class BaseController : Controller
    {
        public const string HubConnectionIDHeader = "HubConnectionId";

        private string _hubConnectionID;
        protected string HubConnectionID
        {
            get
            {
                if (_hubConnectionID == null)
                {
                    _hubConnectionID = Request.Headers[HubConnectionIDHeader];

                    if (_hubConnectionID == null)
                        throw new Exception($"Header '{HubConnectionIDHeader}' is not present in the request.");
                }

                return _hubConnectionID;
            }
        }
    }
}