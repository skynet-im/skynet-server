using SkynetServer.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SkynetServer.Services
{
    internal class ConnectionsService
    {
        private readonly ConcurrentDictionary<long, Client> connections;

        public ConnectionsService()
        {
            connections = new ConcurrentDictionary<long, Client>();
        }

        public bool TryGet(long sessionId, out Client client)
        {
            return connections.TryGetValue(sessionId, out client);
        }

        public Client Add(Client client)
        {
            if (client.SessionId == default) throw new InvalidOperationException();

            Client old = null;

            connections.AddOrUpdate(client.SessionId, client, (sessionId, oldClient) =>
            {
                old = oldClient;
                return client;
            });

            return old;
        }

        public bool TryRemove(long sessionId, out Client client)
        {
            return connections.TryRemove(sessionId, out client);
        }
    }
}
