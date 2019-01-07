using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SkynetServer.Entities;

namespace SkynetServer.Web.Controllers
{
    [Route("~/confirm")]
    public class MailConfirmationController : Controller
    {
        private readonly DatabaseContext _database;

        public MailConfirmationController(DatabaseContext database)
        {
            _database = database;
        }

        [HttpGet("{token}")]
        public IActionResult Get(string token)
        {
            MailConfirmation confirmation = _database.MailConfirmations.Where(x => x.Token.Equals(token, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (confirmation == null)
                return View("Invalid");
            if (confirmation.ConfirmationTime == default(DateTime))
                return View("Pending");
            else
                return View("Complete");
        }

        [HttpPost("{token}")]
        public IActionResult Post(string token)
        {
            MailConfirmation confirmation = _database.MailConfirmations.Where(x => x.Token.Equals(token, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (confirmation == null)
                return View("Invalid");
            if (confirmation.ConfirmationTime == default(DateTime))
            {
                confirmation.ConfirmationTime = DateTime.Now;
                _database.SaveChanges();
                return View("Success");
            }
            else
                return View("Complete");
        }
    }
}