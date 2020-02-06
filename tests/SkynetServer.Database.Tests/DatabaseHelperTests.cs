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

        [ClassInitialize]
        public void Initialize()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("skynetconfig.json", optional: false, reloadOnChange: true)
                .Build();

            var services = new ServiceCollection();
            services.ConfigureSkynet(configuration);
            services.AddDbContext<DatabaseContext>();
            serviceProvider = services.BuildServiceProvider();
        }

        [TestMethod]
        public async Task TestAddAccount()
        {
            await AsyncParallel.ForAsync(0, 500, async i =>
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                var database = scope.ServiceProvider.GetService<DatabaseContext>();
                (_, _, bool success) = await database.AddAccount($"{DatabaseContext.RandomToken()}@example.com", Array.Empty<byte>());
                Assert.IsTrue(success);
            });
        }

        [TestMethod]
        public async Task TestAddExistingAccount()
        {
            const string address = "concurrency@unit.test";
            using IServiceScope scope = serviceProvider.CreateScope();
            var database = scope.ServiceProvider.GetService<DatabaseContext>();
            await database.AddAccount(address, Array.Empty<byte>());
            (_, _, bool success) = await database.AddAccount(address, Array.Empty<byte>());
            Assert.IsFalse(success);
        }

        [TestMethod]
        public async Task TestAddAccountAndSession()
        {
            await AsyncParallel.ForAsync(0, 100, async i =>
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                var database = scope.ServiceProvider.GetService<DatabaseContext>();

                (var account, _, bool success) = await database.AddAccount($"{DatabaseContext.RandomToken()}@example.com", Array.Empty<byte>());
                Assert.IsTrue(success);

                await AsyncParallel.ForAsync(0, 10, j =>
                {
                    using IServiceScope scope = serviceProvider.CreateScope();
                    var database = scope.ServiceProvider.GetService<DatabaseContext>();

                    Session session = new Session()
                    {
                        AccountId = account.AccountId,
                        ApplicationIdentifier = "windows/SkynetServer.Database.Tests"
                    };
                    return database.AddSession(session);
                });
            });
        }

        [TestMethod]
        public async Task TestAddChannel()
        {
            await AsyncParallel.ForAsync(0, 500, i =>
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                var database = scope.ServiceProvider.GetService<DatabaseContext>();

                Channel channel = new Channel() { ChannelType = ChannelType.Loopback };
                return database.AddChannel(channel);
            });
        }

        [TestMethod]
        public async Task TestAddChannelWithOwner()
        {
            await AsyncParallel.ForAsync(0, 50, async i =>
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                var database = scope.ServiceProvider.GetService<DatabaseContext>();

                (var account, _, bool success) = await database.AddAccount($"{DatabaseContext.RandomToken()}@example.com", Array.Empty<byte>());
                Assert.IsTrue(success);

                Channel channel = new Channel()
                {
                    OwnerId = account.AccountId,
                    ChannelType = ChannelType.Loopback
                };
                await database.AddChannel(channel);
            });
        }

        [TestMethod]
        public async Task TestAddChannelAndMessage()
        {
            await AsyncParallel.ForAsync(0, 5, async i =>
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                var database = scope.ServiceProvider.GetService<DatabaseContext>();

                Channel channel = new Channel() { ChannelType = ChannelType.Loopback };
                await database.AddChannel(channel);

                await AsyncParallel.ForAsync(0, 100, j =>
                {
                    using IServiceScope scope = serviceProvider.CreateScope();
                    var database = scope.ServiceProvider.GetService<DatabaseContext>();

                    Message message = new Message() { ChannelId = channel.ChannelId };
                    return database.AddMessage(message);
                });
            });
        }

        [TestMethod]
        public async Task TestAddMessageAndDependency()
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            var database = scope.ServiceProvider.GetService<DatabaseContext>();

            Channel channel = new Channel() { ChannelType = ChannelType.Loopback };
            await database.AddChannel(channel);
            Message previous = null;

            await AsyncParallel.ForAsync(0, 100, async i =>
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                var database = scope.ServiceProvider.GetService<DatabaseContext>();

                Message message = new Message() { ChannelId = channel.ChannelId };
                message = await database.AddMessage(message);
                if (previous != null)
                {
                    database.MessageDependencies.Add(new MessageDependency
                    {
                        OwningMessageId = message.MessageId,
                        MessageId = previous.MessageId,
                    });
                    await database.SaveChangesAsync();
                }
                previous = message;
            });
        }
    }
}
