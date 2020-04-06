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

            await queue.Insert(stream(), 1).ConfigureAwait(false);

            Assert.IsTrue(executed);
        }
    }
}
