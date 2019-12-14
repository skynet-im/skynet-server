using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkynetServer.Model;
using SkynetServer.Network;
using SkynetServer.Network.Packets;
using SkynetServer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkynetServer.Tests
{
    [TestClass]
    public class PacketTests
    {
        private PacketService packets;

        [ClassInitialize]
        public void Constructor()
        {
            packets = new PacketService();
        }

        [TestMethod]
        public void TestInitialize()
        {
            Packet[] packets = this.packets.Packets;
            for (int i = 0; i < packets.Length; i++)
            {
                if (packets[i] == null) continue;
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
    }
}
