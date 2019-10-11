using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkynetServer.Configuration;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Model;
using SkynetServer.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Wiry.Base32;

namespace SkynetServer.Shared.Tests
{
    [TestClass]
    public class DatabaseHelperTests
    {
        [AssemblyInitialize]
        //[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required to match test framework's expected signature")]
        [SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Required to match test framework's expected signature")]
        public static void AssemblyInitialize(TestContext context)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("skynetconfig.json", optional: false, reloadOnChange: true)
                .Build();

            DatabaseContext.ConnectionString = configuration.Get<SkynetOptions>().DatabaseOptions.ConnectionString;

            using DatabaseContext ctx = new DatabaseContext();
            ctx.Database.EnsureCreated();
        }

        [TestMethod]
        public async Task TestAddAccount()
        {
            await AsyncParallel.ForAsync(0, 500, async i =>
            {
                (_, _, bool success) = await DatabaseHelper.AddAccount($"{RandomAddress()}@example.com", Array.Empty<byte>());
                Assert.IsTrue(success);
            });
        }

        [TestMethod]
        public async Task TestAddExistingAccount()
        {
            const string address = "concurrency@unit.test";
            await DatabaseHelper.AddAccount(address, Array.Empty<byte>());
            (_, _, bool success) = await DatabaseHelper.AddAccount(address, Array.Empty<byte>());
            Assert.IsFalse(success);
        }

        [TestMethod]
        public async Task TestAddAccountAndSession()
        {
            await AsyncParallel.ForAsync(0, 100, async i =>
            {
                (var account, _, bool success) = await DatabaseHelper.AddAccount($"{RandomAddress()}@example.com", Array.Empty<byte>());
                Assert.IsTrue(success);

                await AsyncParallel.ForAsync(0, 10, j =>
                {
                    Session session = new Session()
                    {
                        AccountId = account.AccountId,
                        ApplicationIdentifier = "windows/SkynetServer.Database.Tests"
                    };
                    return DatabaseHelper.AddSession(session);
                });
            });
        }

        [TestMethod]
        public async Task TestAddChannel()
        {
            await AsyncParallel.ForAsync(0, 500, i =>
            {
                Channel channel = new Channel() { ChannelType = ChannelType.Loopback };
                return DatabaseHelper.AddChannel(channel);
            });
        }

        [TestMethod]
        public async Task TestAddChannelWithOwner()
        {
            await AsyncParallel.ForAsync(0, 50, async i =>
            {
                (var account, _, bool success) = await DatabaseHelper.AddAccount($"{RandomAddress()}@example.com", Array.Empty<byte>());
                Assert.IsTrue(success);

                Channel channel = new Channel()
                {
                    OwnerId = account.AccountId,
                    ChannelType = ChannelType.Loopback
                };
                await DatabaseHelper.AddChannel(channel);
            });
        }

        [TestMethod]
        public async Task TestAddChannelAndMessage()
        {
            await AsyncParallel.ForAsync(0, 5, async i =>
            {
                Channel channel = new Channel() { ChannelType = ChannelType.Loopback };
                await DatabaseHelper.AddChannel(channel);

                await AsyncParallel.ForAsync(0, 100, j =>
                {
                    Message message = new Message() { ChannelId = channel.ChannelId };
                    return DatabaseHelper.AddMessage(message);
                });
            });
        }

        [TestMethod]
        public async Task TestAddMessageAndDependency()
        {
            Channel channel = new Channel() { ChannelType = ChannelType.Loopback };
            await DatabaseHelper.AddChannel(channel);
            Message previous = null;

            await AsyncParallel.ForAsync(0, 100, async i =>
            {
                Message message = new Message() { ChannelId = channel.ChannelId };
                message = await DatabaseHelper.AddMessage(message);
                if (previous != null)
                {
                    using DatabaseContext ctx = new DatabaseContext();
                    ctx.MessageDependencies.Add(new MessageDependency()
                    {
                        OwningChannelId = channel.ChannelId,
                        OwningMessageId = message.MessageId,
                        ChannelId = channel.ChannelId,
                        MessageId = previous.MessageId,
                    });
                    await ctx.SaveChangesAsync();
                }
                previous = message;
            });
        }

        private string RandomAddress()
        {
            using var random = RandomNumberGenerator.Create();
            byte[] value = new byte[10];
            random.GetBytes(value);
            return Base32Encoding.Standard.GetString(value).ToLowerInvariant();
        }
    }
}
