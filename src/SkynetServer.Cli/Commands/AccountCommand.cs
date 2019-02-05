using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using SkynetServer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkynetServer.Cli.Commands
{
    [Command("account")]
    [Subcommand(typeof(Create), typeof(Confirm))]
    internal class AccountCommand : CommandBase
    {
        [Command("create")]
        internal class Create : CommandBase
        {
            [Option("--id", CommandOptionType.SingleValue)]
            public long AccountId { get; set; }

            [Argument(0)]
            public string AccountName { get; set; }

            private int OnExecute(IConsole console)
            {
                console.Out.WriteLine("WARNING: Argon2 hash is currently not supported!");

                using (DatabaseContext ctx = new DatabaseContext())
                {
                    var account = new Account() { AccountName = AccountName, KeyHash = new byte[0] };

                    try
                    {
                        if (AccountId != 0)
                        {
                            account.AccountId = AccountId;
                            ctx.Accounts.Add(account);
                            ctx.SaveChanges();
                        }
                        else
                        {
                            account = DatabaseHelper.AddAccount(account);
                        }
                        console.Out.WriteLine($"Created account with ID {account.AccountId}");
                        var confirmation = DatabaseHelper.AddMailConfirmation(account, AccountName);
                        console.Out.WriteLine($"Visit https://api.skynet-messenger.com/confirm/{confirmation.Token} to confirm the mail address");
                        return 0;
                    }
                    catch (DbUpdateException ex)
                    {
                        if (ex.InnerException is MySqlException inner && inner.Number == 1062)
                        {
                            console.Error.WriteLine($"The AccountName {AccountName} already exists!");
                        }
                        else
                        {
                            console.Error.WriteLine(ex);
                        }
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

            private int OnExecute(IConsole console)
            {
                using (DatabaseContext context = new DatabaseContext())
                {
                    MailConfirmation confirmation = context.MailConfirmations.SingleOrDefault(c => c.MailAddress == MailAddress);
                    if (confirmation != null)
                    {
                        if (confirmation.ConfirmationTime == default(DateTime))
                        {
                            confirmation.ConfirmationTime = DateTime.Now;
                            context.SaveChanges();
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
