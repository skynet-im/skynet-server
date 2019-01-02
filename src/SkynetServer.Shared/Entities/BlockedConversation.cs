using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Entities
{
    public class BlockedConversation
    {
        public long ChannelId { get; set; }
        public Channel Channel { get; set; }

        public long OwnerId { get; set; }
        public Account Owner { get; set; }
    }
}
