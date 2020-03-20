using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Utilities;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

#pragma warning disable IDE0051 // Remove unused private members

namespace SkynetServer.Commands
{
    [Command("database")]
    [Subcommand(typeof(Create), typeof(Delete), typeof(Benchmark))]
    [HelpOption]
    public class DatabaseCommand
    {
        private int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 1;
        }

        [Command("create", Description = "Creates the database if it does not exist")]
        [HelpOption]
        public class Create
        {
            [Option(Description = "Forces a recreation of the database")]
            public bool Force { get; set; }

            private void OnExecute(DatabaseContext database)
            {
                if (Force && Prompt.GetYesNo("Do you really want to delete the Skynet database?", false))
                {
                    database.Database.EnsureDeleted();
                }

                database.Database.EnsureCreated();
            }
        }

        [Command("delete")]
        [HelpOption]
        public class Delete
        {
            private void OnExecute(DatabaseContext database)
            {
                if (Prompt.GetYesNo("Do you really want to delete the Skynet database?", false))
                {
                    database.Database.EnsureDeleted();
                }
            }
        }

        [Command("benchmark")]
        [HelpOption]
        public class Benchmark
        {
            [Option(CommandOptionType.SingleValue, Description = "Number of accounts to insert")]
            public int AccountCount { get; set; } = 50;

            [Option(CommandOptionType.SingleValue, Description = "Number of messages to insert")]
            public int MessageCount { get; set; } = 100;

            private async Task OnExecute(IConsole console, IServiceProvider provider)
            {
                long accountId, channelId;
                Stopwatch stopwatch = new Stopwatch();

                using (IServiceScope scope = provider.CreateScope())
                {
                    DatabaseContext database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                    (var account, var confirmation, bool success) = await database
                        .AddAccount($"{SkynetRandom.String(10)}@example.com", Array.Empty<byte>()).ConfigureAwait(false);
                    accountId = account.AccountId;
                    console.Out.WriteLine($"Created account {confirmation.MailAddress} with ID {accountId}");
                    console.Out.WriteLine($"Created mail confirmation for {confirmation.MailAddress} with token {confirmation.Token}");
                }

                using (IServiceScope scope = provider.CreateScope())
                {
                    DatabaseContext database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                    Channel channel = await database.AddChannel(new Channel() { OwnerId = accountId }).ConfigureAwait(false);
                    channelId = channel.ChannelId;
                    console.Out.WriteLine($"Created channel {channelId} with owner {accountId}");
                }

                if (AccountCount > 0)
                {
                    console.Out.WriteLine($"Inserting {AccountCount} accounts...");
                    stopwatch.Start();

                    await AsyncParallel.ForAsync(0, AccountCount, async i =>
                    {
                        using IServiceScope scope = provider.CreateScope();
                        DatabaseContext ctx = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                        await ctx.AddAccount($"{SkynetRandom.String(10)}@example.com", Array.Empty<byte>()).ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    stopwatch.Stop();
                    console.Out.WriteLine($"Finished saving {AccountCount} accounts after {stopwatch.ElapsedMilliseconds}ms");
                    stopwatch.Reset();
                }

                if (MessageCount > 0)
                {
                    console.Out.WriteLine($"Inserting {MessageCount} messages to channel {channelId}...");
                    stopwatch.Start();

                    await AsyncParallel.ForAsync(0, MessageCount, async i =>
                    {
                        using IServiceScope scope = provider.CreateScope();
                        DatabaseContext database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                        Message entity = new Message { ChannelId = channelId, SenderId = accountId };
                        database.Messages.Add(entity);
                        await database.SaveChangesAsync().ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    stopwatch.Stop();
                    console.Out.WriteLine($"Finished saving {MessageCount} messages after {stopwatch.ElapsedMilliseconds}ms");
                    stopwatch.Reset();
                }
            }
        }
    }
}
