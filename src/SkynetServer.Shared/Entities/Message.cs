using SkynetServer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Entities
{
    public class Message
    {
        public long MessageId { get; set; }
        public DateTime DispatchTime { get; set; }
        public MessageFlags MessageFlags { get; set; }
        public byte ContentPacketId { get; set; }
        public byte ContentPacketVersion { get; set; }
        public byte[] ContentPacket { get; set; }

        public long ChannelId { get; set; }
        public Channel Channel { get; set; }

        public long? SenderId { get; set; }
        public Account Sender { get; set; }

        public IEnumerable<MessageDependency> Dependencies { get; set; }
    }
}
