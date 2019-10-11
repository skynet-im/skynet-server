using SkynetServer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class MessageAttribute : PacketAttribute
    {
        public MessageAttribute(byte packetId, PacketPolicies packetPolicy)
            : base(packetId, packetPolicy) { }
    }
}
