﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Configuration
{
    internal class ProtocolConfig
    {
        public int ProtocolVersion { get; set; }

        public string VersionName { get; set; }
        public int VersionCode { get; set; }

        public int RecommendUpdateThreshold { get; set; }
        public int ForceUpdateThreshold { get; set; }
    }
}
