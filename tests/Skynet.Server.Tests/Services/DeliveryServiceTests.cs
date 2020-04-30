using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skynet.Model;
using Skynet.Protocol.Packets;
using Skynet.Server.Database;
using Skynet.Server.Database.Entities;
using Skynet.Server.Network;
using Skynet.Server.Services;
using Skynet.Server.Tests.Fakes;
using Skynet.Server.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Skynet.Server.Tests.Services
{
    [TestClass]
    public class DeliveryServiceTests
    {
        private const long alice = 1000;
        private const long bob = 2000;
        private const long charlie = 3000;

        private IServiceProvider serviceProvider;
        private IServiceScope scope;
        private ConnectionsService connections;
        private PacketService packets;
        private DatabaseContext database;
        private DeliveryService delivery;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public async Task PrepareTest()
        {
            serviceProvider = FakeServiceProvider.Create($"{nameof(DeliveryServiceTests)}_{TestContext.TestName}");
            scope = serviceProvider.CreateScope();
            connections = scope.ServiceProvider.GetRequiredService<ConnectionsService>();
            packets = scope.ServiceProvider.GetRequiredService<PacketService>();
            database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            delivery = scope.ServiceProvider.GetRequiredService<DeliveryService>();

            database.Accounts.Add(new Account { AccountId = alice });
            database.MailConfirmations.Add(new MailConfirmation { AccountId = alice, MailAddress = "alice@example.com", Token = SkynetRandom.String(10) });
            database.Accounts.Add(new Account { AccountId = bob });
            database.MailConfirmations.Add(new MailConfirmation { AccountId = bob, MailAddress = "bob@example.com", Token = SkynetRandom.String(10) });
            database.Accounts.Add(new Account { AccountId = charlie });
            database.MailConfirmations.Add(new MailConfirmation { AccountId = charlie, MailAddress = "charlie@example.com", Token = SkynetRandom.String(10) });
            await database.SaveChangesAsync().ConfigureAwait(false);
        }

        [TestCleanup]
        public void DisposeTest()
        {
            (serviceProvider as IDisposable)?.Dispose();
            (scope as IDisposable)?.Dispose();
        }

        [TestMethod]
        public async Task TestSendToAccount()
        {
            Dictionary<long, bool> sent = await CreateSessions(database, connections,
                (alice, 1, true), (alice, 2, false), (alice, 3, true), (alice, 4, true))
                .ConfigureAwait(false);

            var packet = packets.New<P0ACreateChannel>();

            Assert.IsTrue(connections.TryGet(1, out IClient exclude), "Client disappeared from ConnectionsService");
            await delivery.StartSendToAccount(packet, alice, exclude).ConfigureAwait(false);

            Assert.IsFalse(sent[1]);
            Assert.IsFalse(sent[2]);
            Assert.IsTrue(sent[3]);
            Assert.IsTrue(sent[4]);
        }

        [TestMethod]
        public async Task TestSendToChannel()
        {
            Dictionary<long, bool> sent = await CreateSessions(database, connections,
                (alice, 1, true), (alice, 2, false), (alice, 3, true),
                (bob, 4, true), (bob, 5, false), (bob, 6, true),
                (charlie, 7, true), (charlie, 8, false), (charlie, 9, true))
                .ConfigureAwait(false);

            database.Channels.Add(new Channel { ChannelId = 1, ChannelType = ChannelType.AccountData, OwnerId = 10 });
            database.ChannelMembers.Add(new ChannelMember { AccountId = alice, ChannelId = 1 });
            database.ChannelMembers.Add(new ChannelMember { AccountId = bob, ChannelId = 1 });
            await database.SaveChangesAsync().ConfigureAwait(false);

            var packet = packets.New<P0ACreateChannel>();

            Assert.IsTrue(connections.TryGet(1, out IClient exclude), "Client disappeared from ConnectionsService");
            await delivery.StartSendToChannel(packet, 1, exclude).ConfigureAwait(false);

            Assert.IsFalse(sent[1]);
            Assert.IsFalse(sent[2]);
            Assert.IsTrue(sent[3]);
            Assert.IsTrue(sent[4]);
            Assert.IsFalse(sent[5]);
            Assert.IsTrue(sent[6]);
            Assert.IsFalse(sent[7]);
            Assert.IsFalse(sent[8]);
            Assert.IsFalse(sent[9]);
        }

        [TestMethod]
        public async Task TestStartSyncChannels()
        {
            database.Channels.Add(new Channel { ChannelId = 1, ChannelType = ChannelType.Loopback });
            database.ChannelMembers.Add(new ChannelMember { ChannelId = 1, AccountId = alice });
            database.Channels.Add(new Channel { ChannelId = 2, ChannelType = ChannelType.AccountData });
            database.ChannelMembers.Add(new ChannelMember { ChannelId = 2, AccountId = alice });
            database.ChannelMembers.Add(new ChannelMember { ChannelId = 2, AccountId = bob });
            database.ChannelMembers.Add(new ChannelMember { ChannelId = 2, AccountId = charlie });
            database.Channels.Add(new Channel { ChannelId = 3, ChannelType = ChannelType.Direct });
            database.ChannelMembers.Add(new ChannelMember { ChannelId = 3, AccountId = alice });
            database.ChannelMembers.Add(new ChannelMember { ChannelId = 3, AccountId = bob });
            await database.SaveChangesAsync().ConfigureAwait(false);

            var existing = new List<long> { 1, 5 };
            var create = new List<(long channelId, long counterpart)>();
            var delete = new List<long>();
            var client = new FakeClient
            {
                AccountId = alice,
                SessionId = alice + 1,
                OnSendPacket = packet =>
                {
                    if (packet is P0ACreateChannel createChannel)
                        create.Add((createChannel.ChannelId, createChannel.CounterpartId));
                    else if (packet is P0DDeleteChannel deleteChannel)
                        delete.Add(deleteChannel.ChannelId);
                    return Task.CompletedTask;
                }
            };

            await StartSyncChannels(delivery, client, existing, database).ConfigureAwait(false);

            Assert.AreEqual(2, create.Count);
            Assert.AreEqual((2, 0), create[0]);
            Assert.AreEqual((3, bob), create[1]);
            Assert.AreEqual(1, delete.Count);
            Assert.AreEqual(5, delete[0]);
        }

        private async Task<Dictionary<long, bool>> CreateSessions(
            DatabaseContext database,
            ConnectionsService connections,
            params (long accountId, long sessionId, bool connected)[] sessions)
        {
            var sent = new Dictionary<long, bool>();

            foreach (var (accountId, sessionId, connected) in sessions)
            {
                database.Sessions.Add(new Session
                {
                    AccountId = accountId,
                    SessionId = sessionId,
                    SessionTokenHash = SkynetRandom.Bytes(32),
                    WebTokenHash = SkynetRandom.Bytes(32)
                });
                sent.Add(sessionId, false);

                if (connected)
                {
                    connections.Add(new FakeClient
                    {
                        AccountId = accountId,
                        SessionId = sessionId,
                        OnSendPacket = packet =>
                        {
                            sent[sessionId] = true;
                            return Task.CompletedTask;
                        }
                    });
                }
            }

            await database.SaveChangesAsync().ConfigureAwait(false);

            return sent;
        }

        internal static Task StartSyncChannels(DeliveryService instance, IClient client, IReadOnlyList<long> currentState, DatabaseContext database)
        {
            MethodInfo method = instance.GetType().GetMethod("StartSyncChannels", BindingFlags.Instance | BindingFlags.NonPublic);
            return method.Invoke(instance, new object[] { client, currentState, database }) as Task;
        }
    }
}
