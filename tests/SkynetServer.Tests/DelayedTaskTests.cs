using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkynetServer.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkynetServer.Tests
{
    [TestClass]
    public class DelayedTaskTests
    {
        [DataTestMethod]
        [DataRow(-1)]
        [DataRow(100)]
        [DataRow(1000)]
        public async Task TestCancel(int timeout)
        {
            bool executed = false;

            var delayedTask = new DelayedTask(() =>
            {
                executed = true;
                return Task.CompletedTask;
            }, 500);

            Assert.IsNull(delayedTask.Canceled);

            await Task.Delay(100).ConfigureAwait(false);
            delayedTask.Cancel();

            if (timeout < 0)
                await delayedTask.Task.ConfigureAwait(false);
            else
                await Task.Delay(timeout).ConfigureAwait(false);

            Assert.IsTrue(delayedTask.Canceled ?? false);
            Assert.IsFalse(executed);
        }

        [DataTestMethod]
        [DataRow(-1)]
        [DataRow(100)]
        [DataRow(1000)]
        public async Task TestTimeout(int timeout)
        {
            bool executed = false;

            var delayedTask = new DelayedTask(() =>
            {
                executed = true;
                return Task.CompletedTask;
            }, 100);

            Assert.IsNull(delayedTask.Canceled);

            if (timeout < 0)
                await delayedTask.Task.ConfigureAwait(false);
            else
                await Task.Delay(100 + timeout).ConfigureAwait(false);

            Assert.IsFalse(delayedTask.Canceled ?? true);
            Assert.IsTrue(executed);
        }
    }
}
