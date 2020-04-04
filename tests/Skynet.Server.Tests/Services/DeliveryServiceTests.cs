using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            var serviceDescriptors = new ServiceCollection();
            serviceDescriptors.ConfigureSkynetEmpty();
            serviceDescriptors.AddSingleton<IFirebaseService, FakeFirebaseService>();
            serviceDescriptors.AddSingleton<ConnectionsService>();
            serviceDescriptors.AddSingleton<PacketService>();
            serviceDescriptors.AddSingleton<NotificationService>();
            serviceDescriptors.AddTestDatabaseContext($"{nameof(MessageInjectionServiceTests)}_{nameof(TestSendToAccount)}");
            serviceDescriptors.AddScoped<DeliveryService>();
            await using var serviceProvider = serviceDescriptors.BuildServiceProvider();

            using IServiceScope scope = serviceProvider.CreateScope();
            var connections = scope.ServiceProvider.GetRequiredService<ConnectionsService>();
            var packets = scope.ServiceProvider.GetRequiredService<PacketService>();
            var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var delivery = scope.ServiceProvider.GetRequiredService<DeliveryService>();

            database.Accounts.Add(new Account { AccountId = 10 });

            var sent = new Dictionary<long, bool>();
            for (int i = 1; i <= 4; i++)
            {
                int id = i;
                database.Sessions.Add(new Session { AccountId = 10, SessionId = id, WebToken = SkynetRandom.String(30) });
                sent.Add(id, false);
                var client = new FakeClient
                {
                    AccountId = 10,
                    SessionId = id,
                    OnSendPacket = packet =>
                    {
                        sent[id] = true; return Task.CompletedTask;
                    }
                };
                connections.Add(client);
            }
            await database.SaveChangesAsync().ConfigureAwait(false);

            var packet = packets.New<P0ACreateChannel>();

            Assert.IsTrue(connections.TryGet(1, out IClient exclude), "Client disappeared from ConnectionsService");
            await delivery.SendToAccount(packet, 10, exclude).ConfigureAwait(false);

            Assert.IsFalse(sent[1]);
            for (int i = 2; i <= 4; i++)
            {
                Assert.IsTrue(sent[i]);
            }
        }
    }
}
