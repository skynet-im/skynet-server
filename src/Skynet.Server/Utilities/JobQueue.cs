using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Skynet.Server.Utilities
{
    /// <summary>
    /// Provides a self executing job queue with two priority levels.
    /// Enqueuing of async streams is implemented through <see cref="StreamQueue{TItem, TState}"/>.
    /// Jobs are considered to be tentative. This class does not throw <see cref="ObjectDisposedException"/>s.
    /// </summary>
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "JobQueue<T> implements IAsyncDisposable")]
    internal class JobQueue<TItem, TState> : IAsyncDisposable
    {
        private readonly Func<TItem, TState, ValueTask> executor;
        private readonly StreamQueue<TItem, QueueState> queue;
        private readonly StreamQueue<TItem, QueueState> insert;
        private readonly CancellationTokenSource cts;
        private readonly SemaphoreSlim semaphore;
        private readonly object disposeLock;
        private bool executing;
        private bool disposed;

        private static void DisposeItem(QueueState state) => state.Tcs.SetResult(false);

        public JobQueue(Func<TItem, TState, ValueTask> executor)
        {
            this.executor = executor;
            queue = new StreamQueue<TItem, QueueState>(DisposeItem);
            insert = new StreamQueue<TItem, QueueState>(DisposeItem);
            cts = new CancellationTokenSource();
            semaphore = new SemaphoreSlim(1);
            disposeLock = new object();
        }

        /// <summary>
        /// Enqueues a single item to be executed.
        /// </summary>
        public Task Enqueue(TItem item, TState state, bool priority)
        {
            lock (disposeLock)
            {
                if (!disposed)
                {
                    var completionSource = new TaskCompletionSource<bool>();
                    var queueState = new QueueState(completionSource, state);
                    if (priority)
                        insert.Enqueue(item, queueState);
                    else
                        queue.Enqueue(item, queueState);
                    EnsureExecuting();
                    return completionSource.Task;
                }
                else
                {
                    return Task.CompletedTask;
                }
            }
        }

        /// <summary>
        /// Enqueues a collection of items to be executed in the background.
        /// </summary>
        public Task Enqueue(IAsyncEnumerable<TItem> items, TState state, bool priority)
        {
            lock (disposeLock)
            {
                if (!disposed)
                {
                    var completionSource = new TaskCompletionSource<bool>();
                    var queueState = new QueueState(completionSource, state);
                    if (priority)
                        insert.Enqueue(items, queueState);
                    else
                        queue.Enqueue(items, queueState);
                    EnsureExecuting();
                    return completionSource.Task;
                }
                else
                {
                    return Task.CompletedTask;
                }
            }
        }

        /// <summary>
        /// Clears all pending operations from their respective queues and marks them as completed.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            bool disposing = false;

            lock (disposeLock)
            {
                if (!disposed)
                {
                    cts.Cancel();
                    disposing = true;
                    disposed = true;
                }
            }

            if (disposing)
            {
                // Acquire lock to prevent further dequeue operations
                await semaphore.WaitAsync().ConfigureAwait(false);
                await queue.DisposeAsync().ConfigureAwait(false);
                await insert.DisposeAsync().ConfigureAwait(false);
                semaphore.Dispose();
            }
        }

        private async ValueTask<(bool success, TItem item, QueueState state, bool last)> TryDequeue()
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
            try
            {
                await semaphore.WaitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return; // JobQueue is disposing
            }
            catch (ObjectDisposedException)
            {
                return; // JobQueue has been disposed
            }

            if (executing)
            {
                semaphore.Release();
            }
            else
            {
                StartExecution();
            }
        }

        private async void StartExecution()
        {
            var (success, item, state, last) = await TryDequeue().ConfigureAwait(false);
            if (success)
            {
                executing = true;
                semaphore.Release();

                await executor(item, state.State).ConfigureAwait(false);

                if (last) state.Tcs.SetResult(true);

                try
                {
                    await semaphore.WaitAsync(cts.Token).ConfigureAwait(false);
                    StartExecution();
                }
                catch (TaskCanceledException)
                {
                    return; // JobQueue is disposing
                }
                catch (ObjectDisposedException)
                {
                    return; // JobQueue has been disposed
                }
            }
            else
            {
                executing = false;
                semaphore.Release();
            }
        }


        private readonly struct QueueState
        {
            public QueueState(TaskCompletionSource<bool> tcs, TState state)
            {
                Tcs = tcs;
                State = state;
            }

            public TaskCompletionSource<bool> Tcs { get; }
            public TState State { get; }
        }
    }
}
