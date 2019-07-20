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

                bool isMessage = packets[i] is P0BChannelMessage && packets[i].GetType() != typeof(P0BChannelMessage);
                if (!isMessage)
                    Assert.AreEqual(i, packets[i].Id);
                else
                    Assert.AreEqual(i, ((P0BChannelMessage)packets[i]).ContentPacketId);
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
