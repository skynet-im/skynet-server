﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkynetServer.Utilities
{
    internal class StreamQueue<TItem, TState> : IAsyncDisposable
    {
        private readonly ConcurrentQueue<QueueItem> queue;
        private IAsyncEnumerator<TItem> currentEnumerator;
        private TState currentState;

        public StreamQueue()
        {
            queue = new ConcurrentQueue<QueueItem>();
        }

        public void Enqueue(TItem item, TState state)
        {
            if (disposedValue)
                throw new ObjectDisposedException(nameof(StreamQueue<TItem, TState>));

            queue.Enqueue(new QueueItem(item, state));
        }

        public void Enqueue(IAsyncEnumerable<TItem> items, TState state)
        {
            if (disposedValue)
                throw new ObjectDisposedException(nameof(StreamQueue<TItem, TState>));

            queue.Enqueue(new QueueItem(items, state));
        }

        public async ValueTask<(bool success, TItem item, TState state, bool last)> TryDequeue()
        {
            if (disposedValue)
                throw new ObjectDisposedException(nameof(StreamQueue<TItem, TState>));

            if (currentEnumerator != null)
            {
                return await DequeueCached();
            }
            else if (queue.TryDequeue(out QueueItem queueItem))
            {
                if (queueItem.Items == null)
                {
                    return (true, queueItem.Item, queueItem.State, true);
                }
                else
                {
                    // TODO: Provide an API to pass a CancellationToken here
                    IAsyncEnumerator<TItem> enumerator = queueItem.Items.GetAsyncEnumerator();
                    bool next = await enumerator.MoveNextAsync();
                    if (!next)
                    {
                        await enumerator.DisposeAsync();
                        return await TryDequeue();
                    }
                    else
                    {
                        currentEnumerator = enumerator;
                        currentState = queueItem.State;
                        return await DequeueCached();
                    }
                }
            }
            else
            {
                return default;
            }
        }

        private async ValueTask<(bool success, TItem item, TState state, bool last)> DequeueCached()
        {
            TItem item = currentEnumerator.Current;
            TState state = currentState;
            bool last = !await currentEnumerator.MoveNextAsync();
            if (last)
            {
                await currentEnumerator.DisposeAsync();
                currentEnumerator = null;
                currentState = default;
            }
            return (true, item, state, last);
        }

        #region IAsyncDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public async ValueTask DisposeAsync()
        {
            if (!disposedValue)
            {
                if (currentEnumerator != null)
                    await currentEnumerator.DisposeAsync();
                
                disposedValue = true;
            }
        }
        #endregion


        private struct QueueItem
        {
            public TItem Item { get; }
            public IAsyncEnumerable<TItem> Items { get; }
            public TState State { get; }

            public QueueItem(TItem item, TState state) : this()
            {
                Item = item;
                State = state;
            }

            public QueueItem(IAsyncEnumerable<TItem> items, TState state) : this()
            {
                Items = items;
                State = state;
            }
        }
    }
}
