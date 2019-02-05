using SkynetServer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Database.Entities
{
    public class ChannelMember
    {
        public GroupMemberFlags Flags { get; set; }

        public long ChannelId { get; set; }
        public Channel Channel { get; set; }

        public long AccountId { get; set; }
        public Account Account { get; set; }
    }
}
