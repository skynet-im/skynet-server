using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Skynet.Server.Utilities
{
    /// <summary>
    /// Executes a task after the specified delay if not canceled earlier.
    /// </summary>
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Fields are disposed asynchronously.")]
    internal class DelayedTask
    {
        private readonly Func<Task> callback;
        private readonly int millisecondsDelay;
        private readonly CancellationTokenSource cts;
        private volatile bool disposed;

        public DelayedTask(Func<Task> callback, int millisecondsDelay)
        {
            this.callback = callback;
            this.millisecondsDelay = millisecondsDelay;
            cts = new CancellationTokenSource();
            Task = Execute();
        }

        public Task Task { get; }
        public bool? Canceled { get; private set; }

        public void Cancel()
        {
            if (disposed) return;

            try
            {
                cts.Cancel();
                cts.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Ignore this exception and just execute the task
            }
        }

        private async Task Execute()
        {
            try
            {
                await Task.Delay(millisecondsDelay, cts.Token).ConfigureAwait(false);
                Canceled = false;
            }
            catch (TaskCanceledException)
            {
                Canceled = true;
            }
            finally
            {
                disposed = true;
                cts.Dispose();
            }

            if (!Canceled.Value) await callback().ConfigureAwait(false);
        }
    }
}
