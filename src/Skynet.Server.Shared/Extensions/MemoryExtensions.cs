using System;
using System.Collections.Generic;
using System.Text;

namespace Skynet.Server.Extensions
{
    public static class MemoryExtensions
    {
        public static bool SequenceEqual(this byte[] first, ReadOnlySpan<byte> second)
        {
            return new Span<byte>(first).SequenceEqual(second);
        }
    }
}
