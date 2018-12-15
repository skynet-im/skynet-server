using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Packets
{
    public enum CreateAccountError
    {
        Success,
        AccountNameTaken,
        InvalidAccountName
    }
}
