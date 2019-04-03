using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Configuration
{
    public class SkynetConfig
    {
        public MailConfig MailConfig { get; set; }
        public ProtocolConfig ProtocolConfig { get; set; }
        public VslConfig VslConfig { get; set; }

        public string DbConnectionString { get; set; }
    }
}
