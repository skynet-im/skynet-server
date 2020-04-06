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
using System.Threading.Tasks;

namespace Skynet.Server.Tests.Services
{
    [TestClass]
    public class DeliveryServiceTests
    {
        [TestMethod]
        public async Task TestSendToAccount()
        {
            IServiceProvider serviceProvider = FakeServiceProvider.Create($"{nameof(DeliveryServiceTests)}_{nameof(TestSendToAccount)}");
            using IServiceScope scope = serviceProvider.CreateScope();
            var connections = scope.ServiceProvider.GetRequiredService<ConnectionsService>();
            var packets = scope.ServiceProvider.GetRequiredService<PacketService>();
            var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var delivery = scope.ServiceProvider.GetRequiredService<DeliveryService>();

            Dictionary<long, bool> sent = await CreateSessions(database, connections,
                (10, 1, true), (10, 2, false), (10, 3, true), (10, 4, true))
                .ConfigureAwait(false);

            var packet = packets.New<P0ACreateChannel>();

            Assert.IsTrue(connections.TryGet(1, out IClient exclude), "Client disappeared from ConnectionsService");
            await delivery.SendToAccount(packet, 10, exclude).ConfigureAwait(false);

            Assert.IsFalse(sent[1]);
            Assert.IsFalse(sent[2]);
            Assert.IsTrue(sent[3]);
            Assert.IsTrue(sent[4]);
        }

        [TestMethod]
        public async Task TestSendToChannel()
        {
            IServiceProvider serviceProvider = FakeServiceProvider.Create($"{nameof(DeliveryServiceTests)}_{nameof(TestSendToChannel)}");
            using IServiceScope scope = serviceProvider.CreateScope();
            var connections = scope.ServiceProvider.GetRequiredService<ConnectionsService>();
            var packets = scope.ServiceProvider.GetRequiredService<PacketService>();
            var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var delivery = scope.ServiceProvider.GetRequiredService<DeliveryService>();

            Dictionary<long, bool> sent = await CreateSessions(database, connections,
                (10, 1, true), (10, 2, false), (10, 3, true),
                (20, 4, true), (20, 5, false), (20, 6, true),
                (30, 7, true), (30, 8, false), (30, 9, true))
                .ConfigureAwait(false);

            database.Channels.Add(new Channel { ChannelId = 1, ChannelType = ChannelType.AccountData, OwnerId = 10 });
            database.ChannelMembers.Add(new ChannelMember { AccountId = 10, ChannelId = 1 });
            database.ChannelMembers.Add(new ChannelMember { AccountId = 20, ChannelId = 1 });
            await database.SaveChangesAsync().ConfigureAwait(false);

            var packet = packets.New<P0ACreateChannel>();

            Assert.IsTrue(connections.TryGet(1, out IClient exclude), "Client disappeared from ConnectionsService");
            await delivery.SendToChannel(packet, 1, exclude).ConfigureAwait(false);

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

        private async Task<Dictionary<long, bool>> CreateSessions(
            DatabaseContext database, 
            ConnectionsService connections,
            params (long accountId, long sessionId, bool connected)[] sessions)
        {
            var sent = new Dictionary<long, bool>();
            long lastAccountId = default;

            foreach (var (accountId, sessionId, connected) in sessions)
            {
                if (accountId != lastAccountId)
                {
                    database.Accounts.Add(new Account { AccountId = accountId });
                    lastAccountId = accountId;
                }

                database.Sessions.Add(new Session { AccountId = accountId, SessionId = sessionId, WebToken = SkynetRandom.String(30) });
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
    }
}
