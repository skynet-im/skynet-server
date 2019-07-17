using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SkynetServer.Configuration
{
    public class MailOptions
    {
        [Required] public string SenderName { get; set; }
        [Required] public string SenderAddress { get; set; }

        [Required] public string SmtpUsername { get; set; }
        [Required] public string SmtpPassword { get; set; }
        public bool UseSsl { get; set; }
        [Required] public string SmtpHost { get; set; }
        public ushort SmtpPort { get; set; }
    }
}
