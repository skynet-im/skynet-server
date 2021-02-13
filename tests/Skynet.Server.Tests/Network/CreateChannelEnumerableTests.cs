using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skynet.Model;
using Skynet.Protocol.Model;
using Skynet.Protocol.Packets;
using Skynet.Server.Database.Entities;
using Skynet.Server.Network.Handlers;
using Skynet.Server.Services;
using System.Threading.Tasks;

namespace Skynet.Server.Tests.Network
{
    [TestClass]
    public class CreateChannelEnumerableTests
    {
        private const long aliceId = 3424130534998942304;
        private const long aliceChannelId = 4079478577325371057;
        private const long bobId = 430238710495827712;
        private const long bobChannelId = 5724703227946736248;
        private const long tempChannelId = -4672962802000541993;

        private PacketService packetService;

        [TestInitialize]
        public void TestInitialize()
        {
            packetService = new PacketService();
        }

        [TestMethod]
        public async Task TestWaitForChannelCreation()
        {
            var tcs = new TaskCompletionSource<Channel>();
            var enumerator = new CreateChannelEnumerable(packetService, tcs.Task, bobId, bobChannelId).GetAsyncEnumerator();
            ValueTask<bool> task = enumerator.MoveNextAsync();
            await Task.Delay(10).ConfigureAwait(false);
            Assert.IsFalse(task.IsCompleted);
            tcs.SetResult(new Channel());
            Assert.IsTrue(await task);
            Assert.IsNotNull(enumerator.Current);
        }

        [TestMethod]
        public async Task TestAliceReturnSuccess()
        {
            var channel = new Channel { ChannelType = ChannelType.Direct, OwnerId = aliceId };
            var enumerator = new CreateChannelEnumerable(packetService, Task.FromResult(channel), bobId, bobChannelId, tempChannelId).GetAsyncEnumerator();
            Assert.IsTrue(await enumerator.MoveNextAsync());
            var adc = enumerator.Current as P0ACreateChannel;
            Assert.IsNotNull(adc, "The returned packet is not of type " + nameof(P0ACreateChannel));
            Assert.AreEqual(bobChannelId, adc.ChannelId);
            Assert.AreEqual(ChannelType.AccountData, adc.ChannelType);
            Assert.IsTrue(await enumerator.MoveNextAsync());
            var response = enumerator.Current as P2FCreateChannelResponse;
            Assert.IsNotNull(response, "The returned packet is not of type " + nameof(P2FCreateChannelResponse));
            Assert.AreEqual(tempChannelId, response.TempChannelId);
            Assert.AreEqual(CreateChannelStatus.Success, response.StatusCode);
            Assert.IsFalse(await enumerator.MoveNextAsync());
        }

        [TestMethod]
        public async Task TestAliceReturnConflict()
        {
            var enumerator = new CreateChannelEnumerable(packetService, Task.FromResult<Channel>(null), bobId, bobChannelId, tempChannelId).GetAsyncEnumerator();
            Assert.IsTrue(await enumerator.MoveNextAsync());
            var response = enumerator.Current as P2FCreateChannelResponse;
            Assert.IsNotNull(response, "The returned packet is not of type " + nameof(P2FCreateChannelResponse));
            Assert.AreEqual(tempChannelId, response.TempChannelId);
            Assert.AreEqual(CreateChannelStatus.AlreadyExists, response.StatusCode);
            Assert.IsFalse(await enumerator.MoveNextAsync());
        }

        [TestMethod]
        public async Task TestAliceBroadcastSuccess()
        {
            var channel = new Channel { ChannelType = ChannelType.Direct, OwnerId = aliceId };
            var enumerator = new CreateChannelEnumerable(packetService, Task.FromResult(channel), bobId, bobChannelId).GetAsyncEnumerator();
            Assert.IsTrue(await enumerator.MoveNextAsync());
            var adc = enumerator.Current as P0ACreateChannel;
            Assert.IsNotNull(adc, "The returned packet is not of type " + nameof(P0ACreateChannel));
            Assert.AreEqual(bobChannelId, adc.ChannelId);
            Assert.AreEqual(ChannelType.AccountData, adc.ChannelType);
            Assert.IsTrue(await enumerator.MoveNextAsync());
            var response = enumerator.Current as P0ACreateChannel;
            Assert.IsNotNull(response, "The returned packet is not of type " + nameof(P0ACreateChannel));
            Assert.AreEqual(ChannelType.Direct, response.ChannelType);
            Assert.AreEqual(bobId, response.CounterpartId);
            Assert.IsFalse(await enumerator.MoveNextAsync());
        }

        [TestMethod]
        public async Task TestAliceBroadcastConflict()
        {
            var enumerator = new CreateChannelEnumerable(packetService, Task.FromResult<Channel>(null), bobId, bobChannelId).GetAsyncEnumerator();
            Assert.IsFalse(await enumerator.MoveNextAsync());
        }

        [TestMethod]
        public async Task TestBobBroadcastSuccess()
        {
            var channel = new Channel { ChannelType = ChannelType.Direct, OwnerId = aliceId };
            var enumerator = new CreateChannelEnumerable(packetService, Task.FromResult(channel), aliceId, aliceChannelId).GetAsyncEnumerator();
            Assert.IsTrue(await enumerator.MoveNextAsync());
            var adc = enumerator.Current as P0ACreateChannel;
            Assert.IsNotNull(adc, "The returned packet is not of type " + nameof(P0ACreateChannel));
            Assert.AreEqual(aliceChannelId, adc.ChannelId);
            Assert.AreEqual(ChannelType.AccountData, adc.ChannelType);
            Assert.IsTrue(await enumerator.MoveNextAsync());
            var response = enumerator.Current as P0ACreateChannel;
            Assert.IsNotNull(response, "The returned packet is not of type " + nameof(P0ACreateChannel));
            Assert.AreEqual(ChannelType.Direct, response.ChannelType);
            Assert.AreEqual(aliceId, response.CounterpartId);
            Assert.IsFalse(await enumerator.MoveNextAsync());
        }

        [TestMethod]
        public async Task TestBobBroadcastConflict()
        {
            var enumerator = new CreateChannelEnumerable(packetService, Task.FromResult<Channel>(null), aliceId, aliceChannelId).GetAsyncEnumerator();
            Assert.IsFalse(await enumerator.MoveNextAsync());
        }
    }
}
