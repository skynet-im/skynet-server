using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkynetServer.Model;
using SkynetServer.Network;
using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkynetServer.Tests
{
    [TestClass]
    public class PacketTests
    {
        [TestMethod]
        public void TestInitialize()
        {
            Packet[] packets = Packet.Packets;
            for (int i = 0; i < packets.Length; i++)
            {
                if (packets[i] == null) continue;
                Assert.AreEqual(i, packets[i].Id);
            }
        }

        [TestMethod]
        public void TestFlags()
        {
            var publicKeys = Packet.New<P18PublicKeys>();
            Assert.AreEqual(MessageFlags.Unencrypted, publicKeys.RequiredFlags);
            Assert.AreEqual(MessageFlags.Unencrypted, publicKeys.AllowedFlags);

            var nickname = Packet.New<P25Nickname>();
            Assert.AreEqual(MessageFlags.None, nickname.RequiredFlags);
            Assert.AreEqual(MessageFlags.Unencrypted, nickname.AllowedFlags);
        }
    }
}
