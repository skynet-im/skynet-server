using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Packets
{
    internal struct SessionInformation
    {
        public long SessionId { get; set; }
        public DateTime CreationTime { get; set; }
        public string ApplicationIdentifier { get; set; }
    }
}
