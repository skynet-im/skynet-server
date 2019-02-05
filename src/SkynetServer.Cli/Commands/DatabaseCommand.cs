﻿using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Wiry.Base32;

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
            [Option(CommandOptionType.SingleValue, Description = "Count of accounts to insert")]
            public int AccountCount { get; set; } = 50;

            [Option(CommandOptionType.SingleValue, Description = "Count of messages to insert")]
            public int MessageCount { get; set; } = 100;

            private async Task OnExecute(IConsole console)
            {
                long accountId, channelId;
                Stopwatch stopwatch = new Stopwatch();

                if (AccountCount > 0)
                {
                    console.Out.WriteLine($"Inserting {AccountCount} accounts...");
                    stopwatch.Start();

                    await AsyncParallel.ForAsync(0, AccountCount, i =>
                    {
                        return DatabaseHelper.AddAccount($"{RandomAddress()}@example.com", new byte[0]);
                    });

                    stopwatch.Stop();
                    console.Out.WriteLine($"Finished saving {AccountCount} accounts after {stopwatch.ElapsedMilliseconds}ms");
                    stopwatch.Reset();
                }

                {
                    (var account, var confirmation, bool success) = await DatabaseHelper.AddAccount($"{RandomAddress()}@example.com", new byte[0]);
                    accountId = account.AccountId;
                    console.Out.WriteLine($"Created account {confirmation.MailAddress} with ID {accountId}");
                    console.Out.WriteLine($"Created mail confirmation for {confirmation.MailAddress} with token {confirmation.Token}");
                }
                {
                    channelId = (await DatabaseHelper.AddChannel(new Channel() { OwnerId = accountId })).ChannelId;
                    console.Out.WriteLine($"Created channel {channelId} with owner {channelId}");
                }

                if (MessageCount > 0)
                {
                    console.Out.WriteLine($"Inserting {MessageCount} messages to channel {channelId}...");
                    stopwatch.Start();

                    await AsyncParallel.ForAsync(0, MessageCount, i =>
                    {
                        return DatabaseHelper.AddMessage(new Message() { ChannelId = channelId, SenderId = accountId, DispatchTime = DateTime.Now });
                    });

                    stopwatch.Stop();
                    console.Out.WriteLine($"Finished saving {MessageCount} messages after {stopwatch.ElapsedMilliseconds}ms");
                    stopwatch.Reset();
                }
            }

            private string RandomAddress()
            {
                using (var random = RandomNumberGenerator.Create())
                {
                    byte[] value = new byte[10];
                    random.GetBytes(value);
                    return Base32Encoding.Standard.GetString(value).ToLower();
                }
            }
        }
    }
}
