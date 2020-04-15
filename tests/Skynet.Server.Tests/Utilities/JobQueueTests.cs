using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skynet.Server.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skynet.Server.Tests.Utilities
{
    [TestClass]
    public class JobQueueTests
    {
        [TestMethod]
        public async Task TestStreamExecution()
        {
            const string text = "Lorem ipsum";
            const int state = 1;
            bool executed = false;
            
            async static IAsyncEnumerable<string> stream()
            {
                await Task.Delay(500).ConfigureAwait(false);
                yield return text;
            }

            var queue = new JobQueue<string, int>((item, x) =>
            {
                Assert.AreEqual(text, item);
                Assert.AreEqual(state, x);
                executed = true;
                return default;
            });

            await queue.Enqueue(stream(), 1, true).ConfigureAwait(false);

            Assert.IsTrue(executed);
        }

        [TestMethod]
        public async Task TestCancellationOnDispose()
        {
            bool[] completed = new bool[2];

            var queue = new JobQueue<string, int>(async (item, x) =>
            {
                await Task.Delay(100).ConfigureAwait(false);
                completed[x] = true;
            });

            Task task1 = queue.Enqueue("Hello World!", 0, false);
            Task task2 = queue.Enqueue("Goodbye!", 1, false);

            await queue.DisposeAsync().ConfigureAwait(false);

            await task1.ConfigureAwait(false);
            await task2.ConfigureAwait(false);

            // The first item might execute or not, depending which thread enters the lock first.
            // The second item however must not execute.
            Assert.IsFalse(completed[1]);
        }

        [TestMethod]
        public async Task TestUseAfterDispose()
        {
            var queue = new JobQueue<string, int>(async (item, x) =>
            {
                await Task.Delay(100).ConfigureAwait(false);
            });

            _ = queue.Enqueue("Hello world!", 0, false);
            await Task.Delay(50).ConfigureAwait(false);

            await queue.DisposeAsync().ConfigureAwait(false);

            _ = queue.Enqueue("Use after free xD", 1, false);
        }
    }
}
