using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Packets
{
    public enum RestoreSessionError
    {
        Success,
        InvalidCredentials,
        InvalidSession
    }
}
