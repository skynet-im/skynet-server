using Skynet.Model;
using System;
using System.Collections.Generic;

namespace Skynet.Server.Database.Entities
{
    public class Channel
    {
        public long ChannelId { get; set; }
        public ChannelType ChannelType { get; set; }
        public DateTime CreationTime { get; set; }

        public long? OwnerId { get; set; }
        public Account Owner { get; set; }

        public long? CounterpartId { get; set; }
        public Account Counterpart { get; set; }

        public IEnumerable<BlockedConversation> Blockers { get; set; }
        public IEnumerable<ChannelMember> ChannelMembers { get; set; }
    }
}
