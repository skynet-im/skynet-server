using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Configuration
{
    public class SkynetOptions
    {
        public DatabaseOptions DatabaseOptions { get; set; }
        public FcmOptions FcmOptions { get; set; }
        public ListenerOptions ListenerOptions { get; set; }
        public MailOptions MailOptions { get; set; }
        public ProtocolOptions ProtocolOptions { get; set; }
    }
}
