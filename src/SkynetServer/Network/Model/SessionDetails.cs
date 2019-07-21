using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Model
{
    internal struct SessionDetails
    {
        public long SessionId { get; set; }
        public DateTime LastConnected { get; set; }
        public int LastVersionCode { get; set; }
    }
}
