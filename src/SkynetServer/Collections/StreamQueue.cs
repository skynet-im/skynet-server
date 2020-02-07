using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SkynetServer.Collections
{
    internal class StreamQueue<T>
    {
        private readonly Func<T, ValueTask> executor;
        private readonly ConcurrentQueue<WorkSource> queue;
        private readonly ConcurrentQueue<WorkSource> insert;
        private readonly object executeLock;
        private bool executing;

        public Task<bool> Enqueue(T item)
        {
            var completionSource = new TaskCompletionSource<bool>();

            queue.Enqueue(new WorkSource
            {
                Item = item,
                CompletionSource = completionSource
            });

            EnsureExecuting();

            return completionSource.Task;
        }

        public Task<bool> Enqueue(IAsyncEnumerable<T> items)
        {
            var completionSource = new TaskCompletionSource<bool>();

            queue.Enqueue(new WorkSource
            {
                Items = items,
                CompletionSource = completionSource
            });

            EnsureExecuting();

            return completionSource.Task;
        }

        public Task Insert(T item)
        {
            var completionSource = new TaskCompletionSource<bool>();

            insert.Enqueue(new WorkSource
            {
                Item = item,
                CompletionSource = completionSource
            });

            EnsureExecuting();

            return completionSource.Task;
        }

        private bool TryDequeue(out WorkItem item)
        {
            WorkSource source;
            if (insert.TryPeek(out source))
            {
                // TODO: Dequeue only if single or last item
                throw new NotImplementedException();
            }
            else if (queue.TryPeek(out source))
            {
                // TODO: Dequeue only if single or last item
                 throw new NotImplementedException();
            }
            else
            {
                item = default;
                return false;
            }
        }

        private void EnsureExecuting()
        {
            Monitor.Enter(executeLock);
            if (executing)
            {
                Monitor.Exit(executeLock);
            }
            else
            {
                StartExecution(true);
            }
        }

        private void StartExecution(bool locked)
        {

        }

        private struct WorkSource
        {
            public T Item { get; set; }
            public IAsyncEnumerable<T> Items { get; set; }
            public TaskCompletionSource<bool> CompletionSource { get; set; }
        }

        private struct WorkItem
        {
            public T Item { get; set; }
            public TaskCompletionSource<bool> CompletionSource { get; set; }
        }
    }
}
