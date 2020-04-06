using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Skynet.Server.Extensions
{
    public static class TaskExtensions
    {
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This method logs general exceptions to prevent crashes")]
        public static async void CatchExceptions(this Task task, ILogger logger)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "An unexpected exception occurred in an asynchronous background task.");
            }
        }
    }
}
