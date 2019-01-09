using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Configuration
{
    internal class SkynetConfig
    {
        public MailConfig MailConfig { get; set; }
        public ProtocolConfig ProtocolConfig { get; set; }
        public VslConfig VslConfig { get; set; }
    }
}
