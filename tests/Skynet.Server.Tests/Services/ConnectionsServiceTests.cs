using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skynet.Server.Network;
using Skynet.Server.Services;
using Skynet.Server.Tests.Fakes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skynet.Server.Tests.Services
{
    [TestClass]
    public class ConnectionsServiceTests
    {
        [TestMethod]
        public void TestReplaceOnDuplicate()
        {
            IClient client1 = new FakeClient { AccountId = 1, SessionId = 1 };
            IClient client2 = new FakeClient { AccountId = 1, SessionId = 1 };
            var connections = new ConnectionsService();

            IClient replaced1 = connections.Add(client1);
            Assert.IsNull(replaced1);

            Assert.IsTrue(connections.TryGet(1, out IClient out1));
            Assert.IsTrue(ReferenceEquals(client1, out1));

            IClient replaced2 = connections.Add(client2);
            Assert.IsTrue(ReferenceEquals(client1, replaced2));

            Assert.IsTrue(connections.TryGet(1, out IClient out2));
            Assert.IsTrue(ReferenceEquals(client2, out2));
        }

        [TestMethod]
        public async Task TestWaitDisconnectEmpty()
        {
            var connections = new ConnectionsService();
            connections.ClientConnected();
            connections.ClientDisconnected();
            await connections.WaitDisconnectAll().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task TestWaitDisconnect()
        {
            var connections = new ConnectionsService();
            connections.ClientConnected();
            connections.ClientConnected();
            connections.ClientDisconnected();
            connections.ClientDisconnected();
            connections.ClientConnected();
            bool empty = false;
            var task = connections.WaitDisconnectAll().ContinueWith(_ => Assert.IsTrue(empty), TaskScheduler.Default);
            connections.ClientDisconnected();
            empty = true;
            await task.ConfigureAwait(false);
        }
    }
}
