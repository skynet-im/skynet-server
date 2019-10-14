using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Model
{
    public enum CreateSessionStatus
    {
        Success,
        InvalidCredentials,
        InvalidFcmRegistrationToken,
        UnconfirmedAccount
    }
}
