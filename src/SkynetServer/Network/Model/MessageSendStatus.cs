using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Model
{
    public enum MessageSendStatus
    {
        Success,
        FileNotFound,
        AccessDenied,
        ConcurrentChanges
    }
}
