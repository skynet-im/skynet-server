using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network
{
    [Flags]
    public enum PacketPolicies
    {
        None = 0,
        Receive = 1,
        Send = 2,
        Duplex = Receive | Send,
        Unauthenticated = 4,
        Uninitialized = 8
    }
}
