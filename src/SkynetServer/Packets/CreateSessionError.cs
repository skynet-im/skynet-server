using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Packets
{
    public enum CreateSessionError
    {
        Success,
        InvalidCredentials,
        InvalidFcmRegistrationToken
    }
}
