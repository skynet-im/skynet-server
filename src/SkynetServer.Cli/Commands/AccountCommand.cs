using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SkynetServer.Cli.Commands
{
    [Command("account")]
    [Subcommand(typeof(Create), typeof(Confirm), typeof(Resend))]
    [HelpOption]
    public class AccountCommand
    {
        private int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 1;
        }

        [Command("create")]
        [HelpOption]
        public class Create
        {
            [Argument(0)]
            public string AccountName { get; set; }

            [Option("-m|--send-mail", "Send a confirmation mail", CommandOptionType.NoValue)]
            public bool SendMail { get; set; }

            private async Task<int> OnExecute(IConsole console, MailingService mailingService)
            {
                if (!mailingService.IsValidEmail(AccountName))
                {
                    console.Error.WriteLine("Please use a valid email address!");
                    return 1;
                }

                console.Out.WriteLine("WARNING: Argon2 hash is currently not supported!");

                using DatabaseContext ctx = new DatabaseContext();
                (var account, var confirmation, bool success) = await DatabaseHelper.AddAccount(AccountName, Array.Empty<byte>());
                if (success)
                {
                    console.Out.WriteLine($"Created account with ID {account.AccountId}");
                    console.Out.WriteLine($"Visit https://account.skynet.app/confirm/{confirmation.Token} to confirm the mail address");

                    if (SendMail)
                    {
                        console.Out.WriteLine("Sending confirmation mail...");
                        await mailingService.SendMailAsync(AccountName, confirmation.Token);
                    }
                    return 0;
                }
                else
                {
                    console.Error.WriteLine($"The AccountName {AccountName} already exists!");
                    return 1;
                }
            }
        }

        [Command("confirm")]
        [HelpOption]
        public class Confirm
        {
            [Argument(0)]
            public string MailAddress { get; set; }

            private async Task<int> OnExecute(IConsole console)
            {
                using DatabaseContext ctx = new DatabaseContext();
                MailConfirmation confirmation = await ctx.MailConfirmations.SingleOrDefaultAsync(c => c.MailAddress == MailAddress);
                if (confirmation != null)
                {
                    if (confirmation.ConfirmationTime == default)
                    {
                        confirmation.ConfirmationTime = DateTime.Now;
                        await ctx.SaveChangesAsync();
                    }
                    else
                    {
                        console.Out.WriteLine($"The address {MailAddress} has already been confirmed on {confirmation.ConfirmationTime}");
                    }
                    return 0;
                }
                else
                {
                    console.Error.WriteLine($"There is no confirmation pending for {MailAddress}");
                    return 1;
                }
            }
        }

        [Command("resend")]
        [HelpOption]
        public class Resend
        {
            [Argument(0)]
            public string MailAddress { get; set; }

            private async Task<int> OnExecute(IConsole console, MailingService mailingService)
            {
                using DatabaseContext ctx = new DatabaseContext();
                MailConfirmation confirmation = await ctx.MailConfirmations.SingleOrDefaultAsync(c => c.MailAddress == MailAddress);
                if (confirmation != null)
                {
                    console.Out.WriteLine("Sending confirmation mail...");
                    await mailingService.SendMailAsync(MailAddress, confirmation.Token);
                    return 0;
                }
                else
                {
                    console.Out.WriteLine($"The AccountName {MailAddress} does not exist!");
                    return 1;
                }
            }
        }
    }
}
