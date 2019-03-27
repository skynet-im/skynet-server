using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SkynetServer.Web.Controllers
{
    public class StatusController : Controller
    {
        public IActionResult Index(int statusCode)
        {
            return View();
        }
    }
}