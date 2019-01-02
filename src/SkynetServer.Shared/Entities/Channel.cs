using SkynetServer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Entities
{
    public class Channel
    {
        public long ChannelId { get; set; }
        public ChannelType ChannelType { get; set; }

        public long OwnerId { get; set; }
        public Account Owner { get; set; }

        public long OtherId { get; set; }
        public Account Other { get; set; }

        public IEnumerable<Message> Messages { get; set; }
        public IEnumerable<BlockedConversation> Blockers { get; set; }
    }
}
