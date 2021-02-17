using Skynet.Protocol;
using Skynet.Protocol.Model;
using Skynet.Server.Network;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skynet.Server.Tests.Fakes
{
    internal class FakeClient : IClient
    {
        public Func<Packet, Task> OnSendPacket { get; set; }
        public void OnPacketReceived(IClient client, Packet packet)
        {
            PacketReceived?.Invoke(client, packet);
        }

        public string ApplicationIdentifier { get; set; }
        public int VersionCode { get; set; }
        public long AccountId { get; set; }
        public long SessionId { get; set; }
        public bool SoonActive { get; set; }
        public bool Active { get; set; }
        public long FocusedChannelId { get; set; }
        public ChannelAction ChannelAction { get; set; }

        public event Action<IClient, Packet> PacketReceived;

        public void Initialize(string applicationIdentifier, int versionCode)
        {
            ApplicationIdentifier = applicationIdentifier;
            VersionCode = versionCode;
        }
        public void Authenticate(long accountId, long sessionId)
        {
            AccountId = accountId;
            SessionId = sessionId;
        }

        public Task Send(Packet packet)
        {
            return OnSendPacket?.Invoke(packet) ?? Task.CompletedTask;
        }

        public Task Send(IAsyncEnumerable<Packet> packets)
        {
            return Task.CompletedTask;
        }

        public Task Enqueue(Packet packet)
        {
            return Task.CompletedTask;
        }

        public Task Enqueue(ChannelMessage message)
        {
            return Task.CompletedTask;
        }

        public Task Enqueue(IAsyncEnumerable<ChannelMessage> messages)
        {
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync(bool unregister, bool waitForHandling, bool updateState)
        {
            return default;
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }
    }
}
