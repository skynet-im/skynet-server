using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Model
{
    public enum CreateSessionError
    {
        Success,
        InvalidCredentials,
        InvalidFcmRegistrationToken
    }
}
