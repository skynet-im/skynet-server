using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Model
{
    public enum ChannelType : byte
    {
        Loopback,
        Direct,
        Group,
        ProfileData
    }
}
