using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Configuration
{
    internal class VslConfig
    {
        public ushort TcpPort { get; set; }
        public ushort LatestProductVersion { get; set; }
        public ushort OldestProductVersion { get; set; }
        public string RsaXmlKey { get; set; }
    }
}
