using SkynetServer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Database.Entities
{
    public class Channel
    {
        public long ChannelId { get; set; }
        public ChannelType ChannelType { get; set; }
        public long MessageIdCounter { get; set; }

        public long? OwnerId { get; set; }
        public Account Owner { get; set; }

        public IEnumerable<Message> Messages { get; set; }
        public IEnumerable<BlockedConversation> Blockers { get; set; }
        public IEnumerable<ChannelMember> ChannelMembers { get; set; }
    }
}
