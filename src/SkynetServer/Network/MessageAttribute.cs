using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class MessageAttribute : PacketAttribute
    {
        public MessageAttribute(byte packetId, PacketPolicy packetPolicy)
            : base(packetId, packetPolicy) { }
    }
}
