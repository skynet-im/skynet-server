using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Skynet.Model;
using Skynet.Server.Database;
using Skynet.Server.Database.Entities;
using Skynet.Server.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable IDE0051 // Remove unused private members

namespace Skynet.Server.Commands
{
    [Command("database")]
    [Subcommand(typeof(Create), typeof(Delete), typeof(Audit), typeof(Benchmark))]
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

        [Command("audit")]
        [HelpOption(Description = "Searches for anomalies to find corrupted accounts")]
        public class Audit
        {
            private async Task OnExecute(IConsole console, IServiceProvider serviceProvider, DatabaseContext database)
            {
                await foreach (Account account in database.Accounts
                    .Include(a => a.MailConfirmations)
                    .Include(a => a.OwnedChannels)
                    .AsAsyncEnumerable())
                {
                    MailConfirmation confirmation = account.MailConfirmations.OrderByDescending(c => c.ConfirmationTime).FirstOrDefault();
                    if (confirmation == null)
                    {
                        console.Out.WriteLine($"Account {account.AccountId:x8} is missing a MailConfirmation");
                        continue;
                    }

                    if (confirmation.ConfirmationTime == default)
                    {
                        console.Out.WriteLine($"Account {account.AccountId:x8} has not yet confirmed the address {confirmation.MailAddress}");
                        continue;
                    }

                    Channel SingeOrDefault(IEnumerable<Channel> channels, ChannelType type)
                    {
                        Channel[] value = channels.Where(c => c.ChannelType == type).ToArray();
                        if (value.Length == 0)
                        {
                            console.Out.WriteLine($"Account {confirmation.MailAddress} has no channel of type {type}");
                            return null;
                        }
                        else if (value.Length == 1)
                        {
                            return value[0];
                        }
                        else
                        {
                            console.Out.WriteLine($"Account {confirmation.MailAddress} has mutiple channels of type {type}");
                            return null;
                        }
                    }

                    Channel loopback = SingeOrDefault(account.OwnedChannels, ChannelType.Loopback);
                    Channel accountData = SingeOrDefault(account.OwnedChannels, ChannelType.AccountData);

                    if (loopback == null || accountData == null) continue;

                    using (IServiceScope scope = serviceProvider.CreateScope())
                    {
                        var database2 = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                        if (!await database2.Messages.AsQueryable()
                            .Where(m => m.ChannelId == accountData.ChannelId 
                                && m.SenderId == account.AccountId 
                                && m.PacketId == 0x18).AnyAsync()
                            .ConfigureAwait(false))
                        {
                            console.Out.WriteLine($"Account {confirmation.MailAddress} is missing a keypair");
                            continue;
                        }
                    }

                    console.Out.WriteLine($"Account {confirmation.MailAddress} is healthy");
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
