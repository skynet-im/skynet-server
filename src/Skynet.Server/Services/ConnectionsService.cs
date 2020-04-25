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
        private readonly ConcurrentDictionary<long, IClient> connections;
        private readonly TaskCompletionSource<int> completionSource;
        private int counter;

        public ConnectionsService()
        {
            connections = new ConcurrentDictionary<long, IClient>();
            completionSource = new TaskCompletionSource<int>();
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

        public void IncrementCounter()
        {
            Interlocked.Increment(ref counter);
        }

        public void DecrementCounter()
        {
            if (Interlocked.Decrement(ref counter) == 0)
                completionSource.SetResult(0);
        }

        public Task WaitAll()
        {
            if (counter == 0)
                return Task.CompletedTask;
            else
                return completionSource.Task;
        }
    }
}
