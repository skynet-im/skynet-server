using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Model
{
    public enum CreateChannelError
    {
        Success,
        AlreadyExists,
        Blocked
    }
}
