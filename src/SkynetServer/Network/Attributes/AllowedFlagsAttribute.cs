using SkynetServer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Attributes
{
    /// <summary>
    /// Defines <see cref="MessageFlags"/> which this channel message can have. Any additional flags are forbidden.
    /// Use the <see cref="RequiredFlagsAttribute"/> to define mandatory flags.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class AllowedFlagsAttribute : MessageFlagsAttribute
    {
        public AllowedFlagsAttribute(MessageFlags flags)
            : base(flags) { }
    }
}
