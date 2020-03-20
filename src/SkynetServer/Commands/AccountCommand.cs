using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Services;
using SkynetServer.Utilities;
using System;
using System.Threading.Tasks;

#pragma warning disable IDE0051 // Remove unused private members

namespace SkynetServer.Commands
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

            private async Task<int> OnExecute(IConsole console, DatabaseContext database, MailingService mailingService)
            {
                if (!MailUtilities.IsValidAddress(AccountName))
                {
                    console.Error.WriteLine("Please use a valid email address!");
                    return 1;
                }

                console.Out.WriteLine("WARNING: Argon2 hash is currently not supported!");

                (var account, var confirmation, bool success) = await database.AddAccount(AccountName, Array.Empty<byte>()).ConfigureAwait(false);
                if (success)
                {
                    console.Out.WriteLine($"Created account with ID {account.AccountId}");
                    console.Out.WriteLine($"Visit https://account.skynet.app/confirm/{confirmation.Token} to confirm the mail address");

                    if (SendMail)
                    {
                        console.Out.WriteLine("Sending confirmation mail...");
                        await mailingService.SendMailAsync(AccountName, confirmation.Token).ConfigureAwait(false);
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

            private async Task<int> OnExecute(IConsole console, DatabaseContext database)
            {
                MailConfirmation confirmation = await database.MailConfirmations.AsTracking()
                    .SingleOrDefaultAsync(c => c.MailAddress == MailAddress).ConfigureAwait(false);
                if (confirmation != null)
                {
                    if (confirmation.ConfirmationTime == default)
                    {
                        confirmation.ConfirmationTime = DateTime.Now;
                        await database.SaveChangesAsync().ConfigureAwait(false);
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

            private async Task<int> OnExecute(IConsole console, DatabaseContext database, MailingService mailingService)
            {
                MailConfirmation confirmation = await database.MailConfirmations.SingleOrDefaultAsync(c => c.MailAddress == MailAddress).ConfigureAwait(false);
                if (confirmation != null)
                {
                    console.Out.WriteLine("Sending confirmation mail...");
                    await mailingService.SendMailAsync(MailAddress, confirmation.Token).ConfigureAwait(false);
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
