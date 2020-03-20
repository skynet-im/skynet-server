using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Utilities
{
    public static class AsyncParallel
    {
        public static Task ForAsync(int fromInclusive, int toExclusive, Func<int, Task> body)
        {
            return Task.WhenAll(Enumerable.Range(fromInclusive, toExclusive - fromInclusive).Select(i => body(i)));
        }
    }
}
