using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SkynetServer.Configuration
{
    public class ProtocolOptions
    {
        [Range(2, 2)] public int ProtocolVersion { get; set; }

        [Required] public IEnumerable<Platform> Platforms { get; set; }

        public class Platform
        {
            [Required] public string Name { get; set; }
            public int VersionCode { get; set; }
            [Required] public string VersionName { get; set; }

            public int RecommendUpdateThreshold { get; set; }
            public int ForceUpdateThreshold { get; set; }
        }
    }
}
