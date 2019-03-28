using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using SkynetServer.Web.Models;

namespace SkynetServer.Web.Controllers
{
    [Route("~/status")]
    public class StatusController : Controller
    {
        [HttpGet("{statusCode}")]
        public IActionResult Get(int statusCode)
        {
            return View("Status", new StatusViewModel
            {
                StatusCode = statusCode,
                StatusDescription = ReasonPhrases.GetReasonPhrase(statusCode)
            });
        }
    }
}