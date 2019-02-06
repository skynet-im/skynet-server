﻿using McMaster.Extensions.CommandLineUtils;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Cli.Commands
{
    [Command("account")]
    [Subcommand(typeof(Create), typeof(Confirm))]
    internal class AccountCommand : CommandBase
    {
        [Command("create")]
        internal class Create : CommandBase
        {
            [Argument(0)]
            public string AccountName { get; set; }

            private async Task<int> OnExecute(IConsole console)
            {
                console.Out.WriteLine("WARNING: Argon2 hash is currently not supported!");

                using (DatabaseContext ctx = new DatabaseContext())
                {
                    (var account, var confirmation, bool success) = await DatabaseHelper.AddAccount(AccountName, new byte[0]);
                    if (success)
                    {
                        console.Out.WriteLine($"Created account with ID {account.AccountId}");
                        console.Out.WriteLine($"Visit https://api.skynet-messenger.com/confirm/{confirmation.Token} to confirm the mail address");
                        return 0;
                    }
                    else
                    {
                        console.Error.WriteLine($"The AccountName {AccountName} already exists!");
                        return 1;
                    }
                }
            }
        }

        [Command("confirm")]
        internal class Confirm : CommandBase
        {
            [Argument(0)]
            public string MailAddress { get; set; }

            private async Task<int> OnExecute(IConsole console)
            {
                using (DatabaseContext ctx = new DatabaseContext())
                {
                    MailConfirmation confirmation = ctx.MailConfirmations.SingleOrDefault(c => c.MailAddress == MailAddress);
                    if (confirmation != null)
                    {
                        if (confirmation.ConfirmationTime == default(DateTime))
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
        }
    }
}
