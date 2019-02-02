using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Configuration
{
    internal class MailConfig
    {
        public string SenderName { get; set; }
        public string SenderAddress { get; set; }

        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public bool UseSsl { get; set; }
        public string SmtpHost { get; set; }
        public ushort SmtpPort { get; set; }

        public string ContentTemplate { get; set; }
    }
}
