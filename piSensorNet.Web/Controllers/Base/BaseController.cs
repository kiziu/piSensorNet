using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.AspNet.Mvc;
using piSensorNet.DataModel.Context;
using piSensorNet.Web.Models;

namespace piSensorNet.Web.Controllers.Base
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

        [NotNull]
        protected Func<PiSensorNetDbContext> ContextFactory { get; }

        protected BaseController([NotNull] Func<PiSensorNetDbContext> contextFactory)
        {
            if (contextFactory == null)
                throw new ArgumentNullException(nameof(contextFactory));

            ContextFactory = contextFactory;
        }

        protected JsonResult JsonSuccess()
        {
            return new JsonResult(new JsonResultWrapper<object>((object)null));
        }

        protected JsonResult Json<T>(T result, bool success = true)
        {
            return new JsonResult(new JsonResultWrapper<T>(result)
                                  {
                                      Success = success
                                  });
        }

        protected JsonResult JsonFailure(params string[] errors) => JsonFailure((IReadOnlyList<string>)errors);

        protected JsonResult JsonFailure(IReadOnlyList<string> errors)
        {
            return new JsonResult(new JsonResultWrapper<object>(errors));
        }
    }
}