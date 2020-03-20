using SkynetServer.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SkynetServer.Services
{
    internal sealed class ConnectionsService
    {
        private readonly ConcurrentDictionary<long, IClient> connections;

        public ConnectionsService()
        {
            connections = new ConcurrentDictionary<long, IClient>();
        }

        public bool TryGet(long sessionId, out IClient client)
        {
            return connections.TryGetValue(sessionId, out client);
        }

        public IClient Add(IClient client)
        {
            if (client.SessionId == default) throw new InvalidOperationException();

            IClient old = null;

            connections.AddOrUpdate(client.SessionId, client, (sessionId, oldClient) =>
            {
                old = oldClient;
                return client;
            });

            return old;
        }

        public bool TryRemove(long sessionId, out IClient client)
        {
            return connections.TryRemove(sessionId, out client);
        }
    }
}
