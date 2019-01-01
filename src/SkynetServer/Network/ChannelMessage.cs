using SkynetServer.Model;
using SkynetServer.Network.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network
{
    internal abstract class ChannelMessage : Packet
    {
        public long ChannelId { get; set; }
        public long SenderId { get; set; }
        public long MessageId { get; set; }
        public long SkipCount { get; set; }
        public DateTime DispatchTime { get; set; }
        public MessageFlags MessageFlags { get; set; }
        public long FileId { get; set; }
        public List<MessageDependency> Dependencies { get; set; } = new List<MessageDependency>();
    }
}
