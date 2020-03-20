using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkynetServer.Extensions;
using SkynetServer.Model;
using SkynetServer.Network;
using SkynetServer.Network.Packets;
using SkynetServer.Services;
using System;

namespace SkynetServer.Tests.Services
{
    [TestClass]
    public class PacketServiceTests
    {
        private PacketService packets;

        [TestInitialize]
        public void Initialize()
        {
            packets = new PacketService();
        }

        [TestMethod]
        public void TestPackets()
        {
            ReadOnlySpan<Packet> packets = this.packets.Packets;
            for (int i = 0; i < packets.Length; i++)
            {
                if (packets[i] != null)
                    Assert.AreEqual(i, packets[i].Id);
            }
        }

        [TestMethod]
        public void TestFlags()
        {
            var publicKeys = packets.New<P18PublicKeys>();
            Assert.AreEqual(MessageFlags.Unencrypted, publicKeys.RequiredFlags);
            Assert.AreEqual(MessageFlags.Unencrypted, publicKeys.AllowedFlags);

            var nickname = packets.New<P25Nickname>();
            Assert.AreEqual(MessageFlags.None, nickname.RequiredFlags);
            Assert.AreEqual(MessageFlags.Unencrypted, nickname.AllowedFlags);
        }

        [TestMethod]
        public void TestHandlers()
        {
            ReadOnlySpan<Packet> packets = this.packets.Packets;
            for (int i = 0; i < packets.Length; i++)
            {
                if (packets[i] != null && packets[i].Policies.HasFlag(PacketPolicies.Receive))
                {
                    Type handler = this.packets.Handlers[i];
                    Assert.IsNotNull(handler, $"Could not find a handler for {packets[i].GetType().Name}");

                    Type baseInterface = handler.GetGenericInterface(typeof(PacketHandler<>));
                    Assert.IsTrue(baseInterface.GetGenericArguments()[0].IsAssignableFrom(packets[i].GetType()));
                }
            }
        }
    }
}
