using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SkynetServer.Configuration
{
    public class ListenerOptions
    {
        [Range(0, 65535)] public int TcpPort { get; set; }
        [Range(1, 16)] public int Parallelism { get; set; }
        [Required] public string CertificatePath { get; set; }
    }
}
