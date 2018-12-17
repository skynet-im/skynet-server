using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Packets
{
    internal struct MessageDependency
    {
        public MessageDependency(long accountId, long channelId, long messageId)
        {
            AccountId = accountId;
            ChannelId = channelId;
            MessageId = messageId;
        }

        public long AccountId { get; set; }
        public long ChannelId { get; set; }
        public long MessageId { get; set; }
    }
}
