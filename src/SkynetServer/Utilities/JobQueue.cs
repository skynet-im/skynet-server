using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SkynetServer.Utilities
{
    internal class JobQueue<T>
    {
        private readonly Func<T, ValueTask> executor;
        private readonly StreamQueue<T, TaskCompletionSource<bool>> queue;
        private readonly StreamQueue<T, TaskCompletionSource<bool>> insert;
        private readonly object executeLock;
        private bool executing;

        public JobQueue(Func<T, ValueTask> executor)
        {
            this.executor = executor;
            queue = new StreamQueue<T, TaskCompletionSource<bool>>();
            insert = new StreamQueue<T, TaskCompletionSource<bool>>();
            executeLock = new object();
        }

        public Task Enqueue(T item)
        {
            var completionSource = new TaskCompletionSource<bool>();
            queue.Enqueue(item, completionSource);
            EnsureExecuting();
            return completionSource.Task;
        }

        public Task Enqueue(IAsyncEnumerable<T> items)
        {
            var completionSource = new TaskCompletionSource<bool>();
            queue.Enqueue(items, completionSource);
            EnsureExecuting();
            return completionSource.Task;
        }

        public Task Insert(T item)
        {
            var completionSource = new TaskCompletionSource<bool>();
            insert.Enqueue(item, completionSource);
            EnsureExecuting();
            return completionSource.Task;
        }

        public Task Insert(IAsyncEnumerable<T> items)
        {
            var completionSource = new TaskCompletionSource<bool>();
            insert.Enqueue(items, completionSource);
            EnsureExecuting();
            return completionSource.Task;
        }

        private async ValueTask<(bool success, T item, TaskCompletionSource<bool> state, bool last)> TryDequeue()
        {
            var result = await insert.TryDequeue();
            if (result.success)
                return result;

            result = await queue.TryDequeue();
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

            var (success, item, state, last) = await TryDequeue();
            if (success)
            {
                executing = true;
                Monitor.Exit(executeLock);
                await executor(item);
                if (last)
                    state.SetResult(true);
                StartExecution(false);
            }
            else
            {
                executing = false;
                Monitor.Exit(executeLock);
            }
        }
    }
}
