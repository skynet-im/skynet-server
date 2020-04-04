using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Skynet.Server.Utilities
{
    /// <summary>
    /// Provides a self executing job queue with two priority levels.
    /// Enqueuing of async streams is implemented through <see cref="StreamQueue{TItem, TState}"/>.
    /// </summary>
    internal class JobQueue<TItem, TState> : IAsyncDisposable
    {
        private readonly Func<TItem, TState, ValueTask> executor;
        private readonly StreamQueue<TItem, (TaskCompletionSource<bool> tcs, TState state)> queue;
        private readonly StreamQueue<TItem, (TaskCompletionSource<bool> tcs, TState state)> insert;
        private readonly object executeLock;
        private bool executing;

        public JobQueue(Func<TItem, TState, ValueTask> executor)
        {
            this.executor = executor;
            queue = new StreamQueue<TItem, (TaskCompletionSource<bool>, TState)>();
            insert = new StreamQueue<TItem, (TaskCompletionSource<bool>, TState)>();
            executeLock = new object();
        }

        /// <summary>
        /// Enqueues a single item to be executed in the background.
        /// </summary>
        public Task Enqueue(TItem item, TState state)
        {
            var completionSource = new TaskCompletionSource<bool>();
            queue.Enqueue(item, (completionSource, state));
            EnsureExecuting();
            return completionSource.Task;
        }

        /// <summary>
        /// Enqueues a collection of items to be executed in the background.
        /// </summary>
        public Task Enqueue(IAsyncEnumerable<TItem> items, TState state)
        {
            var completionSource = new TaskCompletionSource<bool>();
            queue.Enqueue(items, (completionSource, state));
            EnsureExecuting();
            return completionSource.Task;
        }

        /// <summary>
        /// Schedules a single priority item to be executed as soon as possible.
        /// </summary>
        public Task Insert(TItem item, TState state)
        {
            var completionSource = new TaskCompletionSource<bool>();
            insert.Enqueue(item, (completionSource, state));
            EnsureExecuting();
            return completionSource.Task;
        }

        /// <summary>
        /// Schedules a collection of priority items to be executed as soon as possible.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public Task Insert(IAsyncEnumerable<TItem> items, TState state)
        {
            var completionSource = new TaskCompletionSource<bool>();
            insert.Enqueue(items, (completionSource, state));
            EnsureExecuting();
            return completionSource.Task;
        }

        private async ValueTask<(bool success, TItem item, (TaskCompletionSource<bool> tcs, TState state) state, bool last)> TryDequeue()
        {
            var result = await insert.TryDequeue().ConfigureAwait(false);
            if (result.success)
                return result;

            result = await queue.TryDequeue().ConfigureAwait(false);
            if (result.success)
                return result;

            return default;
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

        private async void StartExecution(bool locked)
        {
            if (!locked)
                Monitor.Enter(executeLock);

            var (success, item, state, last) = await TryDequeue().ConfigureAwait(false);
            if (success)
            {
                executing = true;
                Monitor.Exit(executeLock);
                await executor(item, state.state).ConfigureAwait(false);
                if (last)
                    state.tcs.SetResult(true);
                StartExecution(false);
            }
            else
            {
                executing = false;
                Monitor.Exit(executeLock);
            }
        }

        #region IAsyncDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public async ValueTask DisposeAsync()
        {
            if (!disposedValue)
            {
                await queue.DisposeAsync().ConfigureAwait(false);
                await insert.DisposeAsync().ConfigureAwait(false);

                disposedValue = true;
            }
        }
        #endregion
    }
}
