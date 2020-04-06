using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Skynet.Server.Configuration
{
    public class ListenerOptions
    {
        [Range(0, int.MaxValue)] public int Backlog { get; set; }
        [Range(0, 65535)] public int Port { get; set; }
        [Required] public string CertificatePath { get; set; }
    }
}
