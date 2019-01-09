using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using SkynetServer.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Cli.Commands
{
    [Command("account")]
    [Subcommand(typeof(Create))]
    internal class Account : CommandBase
    {
        [Command("create")]
        internal class Create : CommandBase
        {
            [Argument(0)]
            public string AccountName { get; set; }

            private void OnExecute(IConsole console)
            {
                console.Out.WriteLine("WARNING: Argon2 hash is currently not supported!");

                using (DatabaseContext context = new DatabaseContext())
                {
                    try
                    {
                        var account = context.AddAccount(new Entities.Account() { AccountName = AccountName, KeyHash = new byte[0] });
                        console.Out.WriteLine($"Created account with ID {account.AccountId}");
                        var confirmation = context.AddMailConfirmation(account, AccountName);
                        console.Out.WriteLine($"Visit https://api.skynet-messenger.com/confirm/{confirmation.Token} to activate it");
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
                    }
                }
            }
        }
    }
}
