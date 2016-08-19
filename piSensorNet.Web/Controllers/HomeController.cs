using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.AspNet.Mvc;
using piSensorNet.DataModel.Context;
using piSensorNet.Web.Controllers.Base;

namespace piSensorNet.Web.Controllers
{
    [Area("Root")]
    public sealed class HomeController : BaseController
    {
        public HomeController([NotNull] Func<PiSensorNetDbContext> contextFactory)
            : base(contextFactory) {}

        [HttpGet]
        public ViewResult Index()
        {
            return View("~/Views/Home/Index.cshtml");
        }
    }
}