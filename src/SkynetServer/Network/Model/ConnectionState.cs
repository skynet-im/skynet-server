using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Model
{
    public enum ConnectionState
    {
        Valid,
        CanUpgrade,
        MustUpgrade
    }
}
