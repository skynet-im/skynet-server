using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkynetServer.Model;
using System;
using System.Collections.Generic;

namespace SkynetServer.Shared.Tests
{
    [TestClass]
    public class EnumExtensionsTest
    {
        [TestMethod]
        public void TestIsInRange()
        {
            Assert.IsTrue(MessageFlags.None.IsInRange(MessageFlags.None, MessageFlags.None));
            Assert.IsTrue((MessageFlags.Loopback | MessageFlags.Unencrypted).IsInRange(MessageFlags.Loopback, MessageFlags.All));

            Assert.IsFalse(MessageFlags.Loopback.IsInRange(MessageFlags.Unencrypted, MessageFlags.Loopback | MessageFlags.Unencrypted));
            Assert.IsFalse((MessageFlags.Loopback | MessageFlags.NoSenderSync).IsInRange(MessageFlags.Loopback, MessageFlags.Loopback));
        }
    }
}
