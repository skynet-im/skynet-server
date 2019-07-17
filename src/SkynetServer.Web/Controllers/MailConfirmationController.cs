﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Web.Models;

namespace SkynetServer.Web.Controllers
{
    [Route("~/confirm")]
    public class MailConfirmationController : Controller
    {
        private readonly DatabaseContext ctx;

        public MailConfirmationController(DatabaseContext database)
        {
            ctx = database;
        }

        [HttpGet("{token}")]
        public IActionResult Get(string token)
        {
            MailConfirmation confirmation = ctx.MailConfirmations.Where(x => x.Token == token).FirstOrDefault();
            if (confirmation == null)
                return View("Invalid");
            if (confirmation.ConfirmationTime == default)
                return View("Pending", new MailConfirmationViewModel() { MailAddress = confirmation.MailAddress });
            else
                return View("Confirmed", new MailConfirmationViewModel() { MailAddress = confirmation.MailAddress });
        }

        [HttpPost("{token}")]
        public IActionResult Post(string token)
        {
            MailConfirmation confirmation = ctx.MailConfirmations.Where(x => x.Token == token).FirstOrDefault();
            if (confirmation == null)
                return View("Invalid");
            if (confirmation.ConfirmationTime == default)
            {
                // Remove confirmations that have become obsolete due to an address change
                // TODO: Add protocol interaction to inform clients about a suceeded address change
                ctx.MailConfirmations.RemoveRange(
                    ctx.MailConfirmations.Where(c => c.AccountId == confirmation.AccountId && c.Token != token));

                confirmation.ConfirmationTime = DateTime.Now;
                ctx.SaveChanges();
                return View("Success", new MailConfirmationViewModel() { MailAddress = confirmation.MailAddress });
            }
            else
                return View("Confirmed", new MailConfirmationViewModel() { MailAddress = confirmation.MailAddress });
        }
    }
}