using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SkynetServer.Configuration
{
    public class WebOptions
    {
        public string PathBase { get; set; }
        public bool AllowProxies { get; set; }
        [Required] public Uri PublicBaseUrl { get; set; }
    }
}
