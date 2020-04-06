using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Skynet.Server.Database;
using Skynet.Server.Database.Entities;
using Skynet.Server.Web.Models;

namespace Skynet.Server.Web.Controllers
{
    [Route("~/api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly DatabaseContext ctx;
        private readonly IStringLocalizer<MailConfirmationController> localizer;

        public ApiController(DatabaseContext ctx, IStringLocalizer<MailConfirmationController> localizer)
        {
            this.ctx = ctx;
            this.localizer = localizer;
        }

        [HttpPost("confirm/{token}")]
        public async Task<IActionResult> Confirm(string token)
        {
            string status;
            string content;
            MailConfirmation confirmation = await ctx.MailConfirmations.AsTracking()
                .SingleOrDefaultAsync(x => x.Token == token).ConfigureAwait(false);
            if (confirmation == null)
            {
                status = "Invalid";
                content = localizer["InvalidContent"];
            }
            else if (confirmation.ConfirmationTime == default)
            {
                // Remove confirmations that have become obsolete due to an address change
                // TODO: Add protocol interaction to inform clients about a suceeded address change
                ctx.MailConfirmations.RemoveRange(
                    ctx.MailConfirmations.Where(c => c.AccountId == confirmation.AccountId && c.Token != token));

                confirmation.ConfirmationTime = DateTime.Now;
                await ctx.SaveChangesAsync().ConfigureAwait(false);
                status = "Success";
                content = localizer["SuccessContent", confirmation.MailAddress];
            }
            else
            {
                status = "Confirmed";
                content = localizer["ConfirmedContent", confirmation.MailAddress];
            }

            return new ObjectResult(new ConfirmationResult
            {
                Title = localizer[status + "Title"],
                Header = localizer[status + "Header"],
                Content = content
            });
        }
    }
}