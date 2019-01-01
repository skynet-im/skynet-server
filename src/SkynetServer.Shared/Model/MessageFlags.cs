using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Model
{
    public enum MessageFlags : byte
    {
        None = 0,
        Loopback = 1,
        Unencrypted = 2,
        FileAttached = 4,
        NoSenderSync = 8
    }
}
