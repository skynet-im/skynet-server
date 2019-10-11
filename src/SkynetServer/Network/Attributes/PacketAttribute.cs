using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    internal class PacketAttribute : Attribute
    {
        public PacketAttribute(byte packedId, PacketPolicies packetPolicies)
        {
            PacketId = packedId;
            PacketPolicy = packetPolicies;
        }

        public byte PacketId { get; }
        public PacketPolicies PacketPolicy { get; }
    }
}
