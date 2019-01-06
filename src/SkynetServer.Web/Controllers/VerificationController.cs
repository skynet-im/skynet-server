using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SkynetServer.Web.Controllers
{
    [Route("~/verification")]
    public class VerificationController : Controller
    {
        [HttpGet("{token}")]
        public IActionResult Get(string token)
        {
            return View("Pending");
        }

        [HttpPost("{token}")]
        public IActionResult Post(string token)
        {
            return View("Success");
        }
    }
}