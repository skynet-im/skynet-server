using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkynetServer.Database;
using SkynetServer.Database.Entities;

namespace SkynetServer.Web.Controllers
{
    [Route("~/api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly DatabaseContext ctx;

        public ApiController(DatabaseContext ctx)
        {
            this.ctx = ctx;
        }

        [HttpPost("confirm/{token}")]
        public async Task<IActionResult> Confirm(string token)
        {
            MailConfirmation confirmation = await ctx.MailConfirmations.SingleOrDefaultAsync(x => x.Token == token);
            if (confirmation == null)
                return new ObjectResult(new { StatusCode = "Invalid" });
            if (confirmation.ConfirmationTime == default)
            {
                // Remove confirmations that have become obsolete due to an address change
                // TODO: Add protocol interaction to inform clients about a suceeded address change
                ctx.MailConfirmations.RemoveRange(
                    ctx.MailConfirmations.Where(c => c.AccountId == confirmation.AccountId && c.Token != token));

                confirmation.ConfirmationTime = DateTime.Now;
                await ctx.SaveChangesAsync();
                return new ObjectResult(new { StatusCode = "Success", confirmation.MailAddress });
            }
            else
                return new ObjectResult(new { StatusCode = "Confirmed", confirmation.MailAddress });
        }
    }
}