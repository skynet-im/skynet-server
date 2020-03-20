using SkynetServer.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Database.Entities
{
    public class Message
    {
        public long MessageId { get; set; }
        public DateTime DispatchTime { get; set; }
        public MessageFlags MessageFlags { get; set; }
        public byte PacketId { get; set; }
        public byte PacketVersion { get; set; }
        public byte[] PacketContent { get; set; }

        public long ChannelId { get; set; }
        public Channel Channel { get; set; }

        public long? SenderId { get; set; }
        public Account Sender { get; set; }

        public IEnumerable<MessageDependency> Dependencies { get; set; }
        public IEnumerable<MessageDependency> Dependants { get; set; }
    }
}
