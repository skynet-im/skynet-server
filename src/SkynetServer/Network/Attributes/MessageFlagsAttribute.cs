using SkynetServer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Attributes
{
    /// <summary>
    /// Defines <see cref="MessageFlags"/> which this channel message must have. Any additional flags are forbidden.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class MessageFlagsAttribute : Attribute
    {
        public MessageFlagsAttribute(MessageFlags flags)
        {
            Flags = flags;
        }

        public MessageFlags Flags { get; }
    }
}
