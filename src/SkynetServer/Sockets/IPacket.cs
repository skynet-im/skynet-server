using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Sockets
{
    internal interface IPacket
    {
        byte Id { get; }
        void ReadPacket(PacketBuffer buffer);
        void WritePacket(PacketBuffer buffer);
    }
}
