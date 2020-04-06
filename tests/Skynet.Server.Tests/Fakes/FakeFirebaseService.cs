using Skynet.Server.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skynet.Server.Tests.Fakes
{
    public class FakeFirebaseService : IFirebaseService
    {
        public Func<string, Task<string>> OnSendAsyncToken { get; set; }

        public Task<string> SendAsync(string token)
        {
            return OnSendAsyncToken?.Invoke(token) ?? Task.FromResult(string.Empty);
        }
    }
}
