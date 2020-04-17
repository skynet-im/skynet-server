using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skynet.Server.Database;
using Skynet.Server.Database.Entities;
using Skynet.Server.Web.Models;

namespace Skynet.Server.Web.Controllers
{
    [Route("~/confirm")]
    public class MailConfirmationController : Controller
    {
        private readonly DatabaseContext ctx;

        public MailConfirmationController(DatabaseContext ctx)
        {
            this.ctx = ctx;
        }

        [HttpGet("{token}")]
        public async Task<IActionResult> Get(string token)
        {
            MailConfirmation confirmation = await ctx.MailConfirmations.SingleOrDefaultAsync(x => x.Token == token).ConfigureAwait(false);
            if (confirmation == null)
                return View("Invalid");
            if (confirmation.ConfirmationTime == default)
                return View("Pending", new MailConfirmationViewModel(confirmation.MailAddress, token));
            else
                return View("Confirmed", new MailConfirmationViewModel(confirmation.MailAddress, token));
        }

        [HttpPost("{token}")]
        public async Task<IActionResult> Post(string token)
        {
            MailConfirmation confirmation = await ctx.MailConfirmations.AsTracking()
                .SingleOrDefaultAsync(x => x.Token == token).ConfigureAwait(false);
            if (confirmation == null)
                return View("Invalid");
            if (confirmation.ConfirmationTime == default)
            {
                // Remove confirmations that have become obsolete due to an address change
                // TODO: Add protocol interaction to inform clients about a suceeded address change
                ctx.MailConfirmations.RemoveRange(
                    ctx.MailConfirmations.Where(c => c.AccountId == confirmation.AccountId && c.Token != token));

                confirmation.ConfirmationTime = DateTime.Now;
                await ctx.SaveChangesAsync().ConfigureAwait(false);
                return View("Success", new MailConfirmationViewModel(confirmation.MailAddress, token));
            }
            else
                return View("Confirmed", new MailConfirmationViewModel(confirmation.MailAddress, token));
        }
    }
}