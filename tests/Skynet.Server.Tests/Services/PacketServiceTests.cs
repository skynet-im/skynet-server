﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skynet.Model;
using Skynet.Protocol;
using Skynet.Protocol.Packets;
using Skynet.Server.Extensions;
using Skynet.Server.Network;
using Skynet.Server.Services;
using System;

namespace Skynet.Server.Tests.Services
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
            bool empty = true;
            for (int i = 0; i < packets.Length; i++)
            {
                if (packets[i] != null)
                {
                    Assert.AreEqual(i, packets[i].Id);
                    empty = false;
                }
            }
            Assert.IsFalse(empty, "No packets found");
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
                if (packets[i] != null && packets[i].Policies.HasFlag(PacketPolicies.ClientToServer))
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
