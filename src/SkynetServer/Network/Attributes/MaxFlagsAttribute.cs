using SkynetServer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class MaxFlagsAttribute : MsgFlagsAttribute
    {
        public MaxFlagsAttribute(MessageFlags flags)
            : base(flags) { }
    }
}
