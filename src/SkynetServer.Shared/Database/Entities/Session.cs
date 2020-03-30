using System;
using System.Collections.Generic;

namespace SkynetServer.Database.Entities
{
    public class Session
    {
        public long AccountId { get; set; }
        public byte[] SessionToken { get; set; }
        public string WebToken { get; set; }
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
