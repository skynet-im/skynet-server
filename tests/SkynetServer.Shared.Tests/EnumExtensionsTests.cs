using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkynetServer.Model;
using System;
using System.Collections.Generic;

namespace SkynetServer.Tests
{
    [TestClass]
    public class EnumExtensionsTest
    {
        [TestMethod]
        public void TestAreValid()
        {
            Assert.IsTrue(MessageFlags.None.AreValid(MessageFlags.None, MessageFlags.None));
            Assert.IsTrue((MessageFlags.Loopback | MessageFlags.Unencrypted).AreValid(MessageFlags.Loopback, MessageFlags.All));

            Assert.IsFalse(MessageFlags.Loopback.AreValid(MessageFlags.Unencrypted, MessageFlags.Loopback | MessageFlags.Unencrypted));
            Assert.IsFalse((MessageFlags.Loopback | MessageFlags.NoSenderSync).AreValid(MessageFlags.Loopback, MessageFlags.Loopback));
        }
    }
}
