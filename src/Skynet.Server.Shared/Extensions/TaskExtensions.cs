using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Skynet.Server.Extensions
{
    public static class TaskExtensions
    {
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
