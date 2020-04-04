using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkynetServer.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkynetServer.Tests.Utilities
{
    [TestClass]
    public class JobQueueTests
    {
        [TestMethod]
        public async Task TestStreamExecution()
        {
            const string text = "Lorem ipsum";
            bool executed = false;
            
            async static IAsyncEnumerable<string> stream()
            {
                await Task.Delay(500).ConfigureAwait(false);
                yield return text;
            }

            var queue = new JobQueue<string>(x =>
            {
                Assert.AreEqual(text, x);
                executed = true;
                return default;
            });

            await queue.Insert(stream()).ConfigureAwait(false);

            Assert.IsTrue(executed);
        }
    }
}
