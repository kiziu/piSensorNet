using System;
using System.Linq;
using Microsoft.AspNet.Mvc;

namespace piSensorNet.Web.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}