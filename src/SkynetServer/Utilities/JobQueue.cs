using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace SkynetServer.Utilities
{
    /// <summary>
    /// Provides a self executing job queue with two priority levels.
    /// Enqueuing of async streams is implemented through <see cref="StreamQueue{TItem, TState}"/>.
    /// </summary>
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "JobQueue<T> implements IAsyncDisposable")]
    internal class JobQueue<T> : IAsyncDisposable
    {
        private readonly Func<T, ValueTask> executor;
        private readonly StreamQueue<T, TaskCompletionSource<bool>> queue;
        private readonly StreamQueue<T, TaskCompletionSource<bool>> insert;
        private readonly SemaphoreSlim semaphore;
        private bool executing;

        public JobQueue(Func<T, ValueTask> executor)
        {
            this.executor = executor;
            queue = new StreamQueue<T, TaskCompletionSource<bool>>();
            insert = new StreamQueue<T, TaskCompletionSource<bool>>();
            semaphore = new SemaphoreSlim(1);
        }

        /// <summary>
        /// Enqueues a single item to be executed in the background.
        /// </summary>
        public Task Enqueue(T item)
        {
            var completionSource = new TaskCompletionSource<bool>();
            queue.Enqueue(item, completionSource);
            EnsureExecuting();
            return completionSource.Task;
        }

        /// <summary>
        /// Enqueues a collection of items to be executed in the background.
        /// </summary>
        public Task Enqueue(IAsyncEnumerable<T> items)
        {
            var completionSource = new TaskCompletionSource<bool>();
            queue.Enqueue(items, completionSource);
            EnsureExecuting();
            return completionSource.Task;
        }

        /// <summary>
        /// Schedules a single priority item to be executed as soon as possible.
        /// </summary>
        public Task Insert(T item)
        {
            var completionSource = new TaskCompletionSource<bool>();
            insert.Enqueue(item, completionSource);
            EnsureExecuting();
            return completionSource.Task;
        }

        /// <summary>
        /// Schedules a collection of priority items to be executed as soon as possible.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public Task Insert(IAsyncEnumerable<T> items)
        {
            var completionSource = new TaskCompletionSource<bool>();
            insert.Enqueue(items, completionSource);
            EnsureExecuting();
            return completionSource.Task;
        }

        private async ValueTask<(bool success, T item, TaskCompletionSource<bool> state, bool last)> TryDequeue()
        {
            var result = await insert.TryDequeue().ConfigureAwait(false);
            if (result.success)
                return result;

            result = await queue.TryDequeue().ConfigureAwait(false);
            if (result.success)
                return result;

            return default;
        }

        private async void EnsureExecuting()
        {
            await semaphore.WaitAsync().ConfigureAwait(false);
            if (executing)
            {
                semaphore.Release();
            }
            else
            {
                StartExecution(true);
            }
        }

        private async void StartExecution(bool locked)
        {
            if (!locked)
                await semaphore.WaitAsync().ConfigureAwait(false);

            var (success, item, state, last) = await TryDequeue().ConfigureAwait(false);
            if (success)
            {
                executing = true;
                semaphore.Release();
                await executor(item).ConfigureAwait(false);
                if (last)
                    state.SetResult(true);
                StartExecution(false);
            }
            else
            {
                executing = false;
                semaphore.Release();
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
                semaphore.Dispose();

                disposedValue = true;
            }
        }
        #endregion
    }
}
