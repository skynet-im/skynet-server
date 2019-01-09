using System;
using System.Collections.Generic;
using System.Text;
using VSL;

namespace SkynetServer.Network
{
    internal class Client
    {
        VSLServer socket;

        public Client(VSLServer socket)
        {
            this.socket = socket;
        }
    }
}
