using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Model;
using SkynetServer.Network.Packets;
using SkynetServer.Services;
using SkynetServer.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkynetServer.Tests.Services
{
    [TestClass]
    public class MessageInjectionServiceTests
    {
        [TestMethod]
        public async Task TestCreateMessage()
        {
            var serviceDescriptors = new ServiceCollection();
            serviceDescriptors.AddSingleton<PacketService>();
            serviceDescriptors.AddTestDatabaseContext($"{nameof(MessageInjectionServiceTests)}_{nameof(TestCreateMessage)}");
            serviceDescriptors.AddScoped<MessageInjectionService>();
            await using var serviceProvider = serviceDescriptors.BuildServiceProvider();

            using IServiceScope scope = serviceProvider.CreateScope();
            var packets = scope.ServiceProvider.GetRequiredService<PacketService>();
            var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            var injector = scope.ServiceProvider.GetRequiredService<MessageInjectionService>();

            var packet = packets.New<P14MailAddress>();
            packet.MessageFlags = MessageFlags.Unencrypted;
            packet.MailAddress = "unittest@skynet.app";

            long channelId = SkynetRandom.Id();
            long accountId = SkynetRandom.Id();

            await injector.CreateMessage(packet, channelId, accountId).ConfigureAwait(false);

            Message message = await database.Messages.SingleAsync().ConfigureAwait(false);
            Assert.AreEqual(channelId, message.ChannelId);
            Assert.AreEqual(accountId, message.SenderId);
            Assert.AreEqual(MessageFlags.Unencrypted, message.MessageFlags);
        }
    }
}
