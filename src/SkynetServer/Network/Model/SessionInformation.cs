using System;
using System.Collections.Generic;

namespace SkynetServer.Network.Model
{
    internal struct SessionInformation
    {
        public SessionInformation(long sessionId, DateTime creationTime, string applicationIdentifier)
        {
            SessionId = sessionId;
            CreationTime = creationTime;
            ApplicationIdentifier = applicationIdentifier;
        }

        public long SessionId { get; set; }
        public DateTime CreationTime { get; set; }
        public string ApplicationIdentifier { get; set; }
    }
}
