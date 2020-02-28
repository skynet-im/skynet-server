using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Extensions;
using SkynetServer.Model;
using SkynetServer.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkynetServer.Tests
{
    [TestClass]
    public class DatabaseHelperTests
    {
        private IServiceProvider serviceProvider;

        [TestInitialize]
        public void Initialize()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("skynetconfig.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            services.ConfigureSkynet(configuration);
            services.AddDatabaseContext(configuration);
            serviceProvider = services.BuildServiceProvider();
        }

        [TestMethod]
        public async Task TestAddAccount()
        {
            await AsyncParallel.ForAsync(0, 500, async i =>
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                (_, _, bool success) = await database.AddAccount($"{SkynetRandom.String(10)}@example.com", Array.Empty<byte>()).ConfigureAwait(false);
                Assert.IsTrue(success);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TestAddExistingAccount()
        {
            const string address = "concurrency@unit.test";

            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                await database.AddAccount(address, Array.Empty<byte>()).ConfigureAwait(false);
            }

            // If we add conflicting accounts in the same scope, EF Core is throwing an exception.

            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                (_, _, bool success) = await database.AddAccount(address, Array.Empty<byte>()).ConfigureAwait(false);
                Assert.IsFalse(success);
            }
        }

        [TestMethod]
        public async Task TestAddAccountAndSession()
        {
            await AsyncParallel.ForAsync(0, 100, async i =>
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

                (var account, _, bool success) = await database
                    .AddAccount($"{SkynetRandom.String(10)}@example.com", Array.Empty<byte>()).ConfigureAwait(false);
                Assert.IsTrue(success);

                await AsyncParallel.ForAsync(0, 10, async j =>
                {
                    using IServiceScope scope = serviceProvider.CreateScope();
                    var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

                    Session session = new Session()
                    {
                        AccountId = account.AccountId,
                        ApplicationIdentifier = "windows/SkynetServer.Database.Tests"
                    };
                    await database.AddSession(session).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TestAddChannel()
        {
            await AsyncParallel.ForAsync(0, 500, async i =>
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

                Channel channel = new Channel() { ChannelType = ChannelType.Loopback };
                await database.AddChannel(channel).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TestAddChannelWithOwner()
        {
            await AsyncParallel.ForAsync(0, 50, async i =>
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

                (var account, _, bool success) = await database
                    .AddAccount($"{SkynetRandom.String(10)}@example.com", Array.Empty<byte>()).ConfigureAwait(false);
                Assert.IsTrue(success);

                Channel channel = new Channel()
                {
                    OwnerId = account.AccountId,
                    ChannelType = ChannelType.Loopback
                };
                await database.AddChannel(channel).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TestAddChannelAndMessage()
        {
            await AsyncParallel.ForAsync(0, 5, async i =>
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

                Channel channel = new Channel() { ChannelType = ChannelType.Loopback };
                await database.AddChannel(channel).ConfigureAwait(false);

                await AsyncParallel.ForAsync(0, 100, async j =>
                {
                    using IServiceScope scope = serviceProvider.CreateScope();
                    var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

                    Message message = new Message()
                    {
                        ChannelId = channel.ChannelId,
                        MessageFlags = MessageFlags.Unencrypted
                    };
                    database.Messages.Add(message);
                    await database.SaveChangesAsync().ConfigureAwait(false);
                }).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TestAddMessageAndDependency()
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            Channel channel = new Channel() { ChannelType = ChannelType.Loopback };
            await database.AddChannel(channel).ConfigureAwait(false);
            Message previous = null;

            await AsyncParallel.ForAsync(0, 100, async i =>
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

                var dependencies = new List<MessageDependency>();
                if (previous != null)
                    dependencies.Add(new MessageDependency { MessageId = previous.MessageId });

                Message message = new Message()
                {
                    ChannelId = channel.ChannelId,
                    MessageFlags = MessageFlags.Unencrypted,
                    Dependencies = dependencies
                };
              
                await database.SaveChangesAsync().ConfigureAwait(false);
                previous = message;
            }).ConfigureAwait(false);
        }
    }
}
