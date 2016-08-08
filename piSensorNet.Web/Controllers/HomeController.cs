using System;
using System.Linq;
using Microsoft.AspNet.Mvc;
using piSensorNet.Web.Controllers.Base;

namespace piSensorNet.Web.Controllers
{
    public sealed class HomeController : BaseController
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}