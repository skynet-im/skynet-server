using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SkynetServer.Configuration
{
    public class VslOptions
    {
        [Range(0, 65535)] public ushort TcpPort { get; set; }
        [Range(2, 2)] public ushort LatestProductVersion { get; set; }
        [Range(2, 2)] public ushort OldestProductVersion { get; set; }
        [Required] public string RsaXmlKey { get; set; }
    }
}
