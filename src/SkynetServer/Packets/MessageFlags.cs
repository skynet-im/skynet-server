using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Packets
{
   public enum MessageFlags
    {
        None = 0,
        Loopback = 1,
        Unencrypted = 2,
        FileAttached = 4,
        NoSenderSync = 8
    }
}
