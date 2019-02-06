using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Database.Entities
{
    public class MessageDependency
    {
        public long AccountId { get; set; }
        public long ChannelId { get; set; }

        public long MessageId { get; set; }
        public Message Message { get; set; }
    }
}
