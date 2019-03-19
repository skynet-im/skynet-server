using McMaster.Extensions.CommandLineUtils;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Threading;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Wiry.Base32;

namespace SkynetServer.Cli.Commands
{
    [Command("database")]
    [Subcommand(typeof(Create), typeof(Delete), typeof(Benchmark))]
    [HelpOption]
    internal class DatabaseCommand
    {
        private int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 1;
        }

        [Command("create", Description = "Creates the database if it does not exist")]
        [HelpOption]
        internal class Create
        {
            [Option(Description = "Forces a recreation of the database")]
            public bool Force { get; set; }

            private void OnExecute()
            {
                using (DatabaseContext ctx = new DatabaseContext())
                {
                    if (Force && Prompt.GetYesNo("Do you really want to delete the Skynet database?", false))
                    {
                        ctx.Database.EnsureDeleted();
                    }

                    ctx.Database.EnsureCreated();
                }
            }
        }

        [Command("delete")]
        [HelpOption]
        internal class Delete
        {
            private void OnExecute()
            {
                if (Prompt.GetYesNo("Do you really want to delete the Skynet database?", false))
                {
                    using (DatabaseContext ctx = new DatabaseContext())
                    {
                        ctx.Database.EnsureDeleted();
                    }
                }
            }
        }

        [Command("benchmark")]
        [HelpOption]
        internal class Benchmark
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
                        return DatabaseHelper.AddMessage(new Message() { ChannelId = channelId, SenderId = accountId });
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
