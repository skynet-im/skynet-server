﻿using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using SkynetServer.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Cli.Commands
{
    [Command("database")]
    [Subcommand(typeof(Create), typeof(Delete), typeof(Benchmark))]
    internal class DatabaseCommand : CommandBase
    {
        private int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 1;
        }

        [Command("create")]
        internal class Create : CommandBase
        {
            private void OnExecute()
            {
                using (DatabaseContext context = new DatabaseContext())
                {
                    context.Database.EnsureCreated();
                }
            }
        }

        [Command("delete")]
        internal class Delete : CommandBase
        {
            private void OnExecute()
            {
                if (Prompt.GetYesNo("Do you really want to delete the Skynet database?", false))
                {
                    using (DatabaseContext context = new DatabaseContext())
                    {
                        context.Database.EnsureDeleted();
                    }
                }
            }
        }

        [Command("benchmark")]
        internal class Benchmark : CommandBase
        {
            [Option(CommandOptionType.SingleValue, Description = "Count of messages to insert")]
            public int MessageCount { get; set; } = 100;

            private void OnExecute(IConsole console)
            {
                long accountId, channelId;

                using (DatabaseContext ctx = new DatabaseContext())
                {
                    Account account = new Account() { AccountName = $"{new Random().Next()}@example.com", KeyHash = new byte[0] };
                    accountId = ctx.AddAccount(account).AccountId;
                    console.Out.WriteLine($"Created account {account.AccountName} with ID {accountId}");
                    MailConfirmation confirmation = ctx.AddMailConfirmation(account, account.AccountName);
                    console.Out.WriteLine($"Created mail confirmation for {confirmation.MailAddress} with token {confirmation.Token}");
                }

                using (DatabaseContext ctx = new DatabaseContext())
                {
                    channelId = ctx.AddChannel(new Channel() { OwnerId = accountId }).ChannelId;
                    console.Out.WriteLine($"Created channel {channelId} with owner {channelId}");
                }

                console.Out.WriteLine($"Inserting {MessageCount} messages to channel {channelId}...");
                Stopwatch stopwatch = Stopwatch.StartNew();

                Parallel.For(0, MessageCount, i =>
                {
                    using (DatabaseContext ctx = new DatabaseContext())
                    {
                        ctx.AddMessage(new Message() { ChannelId = channelId, SenderId = accountId, DispatchTime = DateTime.Now });
                    }
                });

                stopwatch.Stop();
                console.Out.WriteLine($"Finished saving {MessageCount} messages after {stopwatch.ElapsedMilliseconds}ms");
            }
        }
    }
}