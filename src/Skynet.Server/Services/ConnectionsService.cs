using Skynet.Server.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Skynet.Server.Services
{
    internal sealed class ConnectionsService
    {
        private readonly ConcurrentDictionary<long, IClient> connectedSessions;
        private readonly TaskCompletionSource<int> completionSource;
        private int connectionsCounter;

        public ConnectionsService()
        {
            connectedSessions = new ConcurrentDictionary<long, IClient>();
            completionSource = new TaskCompletionSource<int>();
        }

        public bool TryGet(long sessionId, out IClient client)
        {
            return connectedSessions.TryGetValue(sessionId, out client);
        }

        /// <summary>
        /// Adds a client to the list of connections and returns the client that has been kicked instead.
        /// </summary>
        /// <param name="client">An authenticated client to add.</param>
        public IClient Add(IClient client)
        {
            if (client.SessionId == default) throw new InvalidOperationException();

            IClient old = null;

            connectedSessions.AddOrUpdate(client.SessionId, client, (sessionId, oldClient) =>
            {
                old = oldClient;
                return client;
            });

            return old;
        }

        public bool TryRemove(long sessionId, out IClient client)
        {
            return connectedSessions.TryRemove(sessionId, out client);
        }

        /// <summary>
        /// Increments the connection counter. Make sure to call <see cref="ClientDisconnected"/> after disconnecting.
        /// </summary>
        public void ClientConnected()
        {
            Interlocked.Increment(ref connectionsCounter);
        }

        /// <summary>
        /// Decrements the connections counter and notifies a waiter if all clients have disconnected.
        /// </summary>
        public void ClientDisconnected()
        {
            // A negative counter means that a pending wait operation has completed
            if (Interlocked.Decrement(ref connectionsCounter) < 0)
                completionSource.TrySetResult(0);
        }

        /// <summary>
        /// Waits for all clients to disconnect. This method is not thread safe. Do not call it multiple times.
        /// It might return although a new client has just started its receive loop.
        /// </summary>
        public Task WaitDisconnectAll()
        {
            // Decrement the counter so that it becomes negative when all clients have disconnected
            if (Interlocked.Decrement(ref connectionsCounter) < 0)
                completionSource.TrySetResult(0);

            return completionSource.Task;
        }
    }
}
