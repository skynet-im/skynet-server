using SkynetServer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class MsgFlagsAttribute : Attribute
    {
        public MsgFlagsAttribute(MessageFlags flags)
        {
            Flags = flags;
        }

        public MessageFlags Flags { get; }
    }
}
