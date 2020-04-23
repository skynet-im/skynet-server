using Skynet.Protocol;
using Skynet.Protocol.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skynet.Server.Network
{
    internal interface IClient
    {
        string ApplicationIdentifier { get; }
        int VersionCode { get; }
        long AccountId { get; }
        long SessionId { get; }
        bool SoonActive { get; set; }
        bool Active { get; set; }
        long FocusedChannelId { get; set; }
        ChannelAction ChannelAction { get; set; }

        event Action<IClient, Packet> PacketReceived;

        void Initialize(string applicationIdentifier, int versionCode);
        void Authenticate(long accountId, long sessionId);
        Task Send(Packet packet);
        Task Enqueue(Packet packet);
        Task Enqueue(ChannelMessage message);
        Task Enqueue(IAsyncEnumerable<ChannelMessage> messages);
        ValueTask DisposeAsync(bool unregister = true, bool waitForHandling = true, bool updateState = true);
    }
}
