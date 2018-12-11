using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Packets
{
    public enum ConnectionState
    {
        Valid,
        CanUpgrade,
        MustUpgrade
    }
}
