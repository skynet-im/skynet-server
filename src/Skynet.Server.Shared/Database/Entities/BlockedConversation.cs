using System;
using System.Collections.Generic;

namespace Skynet.Server.Database.Entities
{
    public class BlockedConversation
    {
        public long ChannelId { get; set; }
        public Channel Channel { get; set; }

        public long OwnerId { get; set; }
        public Account Owner { get; set; }
    }
}
