﻿using Skynet.Server.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Skynet.Server.Services
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

        /// <summary>
        /// Adds a client to the list of connections and returns the client that has been kicked instead.
        /// </summary>
        /// <param name="client">An authenticated client to add.</param>
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
