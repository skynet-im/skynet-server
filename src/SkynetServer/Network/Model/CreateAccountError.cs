﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Model
{
    public enum CreateAccountError
    {
        Success,
        AccountNameTaken,
        InvalidAccountName
    }
}
