using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Skynet.Server.Configuration
{
    public class MailOptions
    {
        public bool EnableMailing { get; set; }
        [Required] public string SenderName { get; set; }
        [Required] public string SenderAddress { get; set; }

        [Required] public string SmtpUsername { get; set; }
        [Required(AllowEmptyStrings = true)] public string SmtpPassword { get; set; }
        public bool UseSsl { get; set; }
        [Required] public string SmtpHost { get; set; }
        [Range(0, 65535)] public ushort SmtpPort { get; set; }
    }
}
