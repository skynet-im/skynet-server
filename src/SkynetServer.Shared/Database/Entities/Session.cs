using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Database.Entities
{
    public class Session
    {
        public long AccountId { get; set; }
        public Account Account { get; set; }

        public long SessionId { get; set; }
        public DateTime CreationTime { get; set; }
        public string AppIdentifier { get; set; }
        public string FcmToken { get; set; }
        public DateTime LastFcmMessage { get; set; }
        public DateTime LastConnected { get; set; }
        public int LastVersionCode { get; set; }
    }
}
