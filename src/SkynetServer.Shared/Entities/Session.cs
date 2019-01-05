﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Entities
{
    public class Session
    {
        public long SessionId { get; set; }
        public DateTime CreationTime { get; set; }
        public string ApplicationIdentifier { get; set; }

        public DateTime LastConnected { get; set; }
        public int LastVersionCode { get; set; }

        public long AccountId { get; set; }
        public Account Account { get; set; }
    }
}
