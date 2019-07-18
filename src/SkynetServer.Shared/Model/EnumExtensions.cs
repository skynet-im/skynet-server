using System;
using System.Collections.Generic;

namespace SkynetServer.Model
{
    public static class EnumExtensions
    {
        public static bool IsInRange(this MessageFlags value, MessageFlags minimum, MessageFlags maximum)
        {
            return (value & minimum) == minimum
                && (value | maximum) == maximum;
        }
    }
}
