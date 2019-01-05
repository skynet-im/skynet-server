using SkynetServer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Entities
{
    public class GroupMember
    {
        public GroupMemberFlags Flags { get; set; }

        public long ChannelId { get; set; }
        public Channel Channel { get; set; }

        public long AccountId { get; set; }
        public Account Account { get; set; }
    }
}
