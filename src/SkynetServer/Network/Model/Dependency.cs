using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network.Model
{
    internal struct Dependency
    {
        public Dependency(long accountId, long messageId)
        {
            AccountId = accountId;
            MessageId = messageId;
        }

        public long AccountId { get; set; }
        public long MessageId { get; set; }
    }
}
