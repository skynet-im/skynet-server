using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Extensions
{
    public static class MemoryExtensions
    {
        public static bool SequenceEqual(this byte[] first, ReadOnlySpan<byte> second)
        {
            return new Span<byte>(first).SequenceEqual(second);
        }
    }
}
