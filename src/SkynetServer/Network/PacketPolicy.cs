using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network
{
    [Flags]
    public enum PacketPolicy
    {
        None = 0,
        Receive = 1,
        Send = 2,
        Duplex = Receive | Send,
        Unauthenticated = 4
    }
}
