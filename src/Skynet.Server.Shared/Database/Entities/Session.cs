﻿using System;
using System.Collections.Generic;

namespace Skynet.Server.Database.Entities
{
    public class Session
    {
        public long AccountId { get; set; }
        public byte[] SessionTokenHash { get; set; }
        public byte[] WebTokenHash { get; set; }
        public Account Account { get; set; }

        public long SessionId { get; set; }
        public DateTime CreationTime { get; set; }
        public string ApplicationIdentifier { get; set; }
        public string FcmToken { get; set; }
        public DateTime LastFcmMessage { get; set; }
        public DateTime LastConnected { get; set; }
        public int LastVersionCode { get; set; }
    }
}
