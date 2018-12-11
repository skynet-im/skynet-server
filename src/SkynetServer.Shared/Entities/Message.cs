using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Entities
{
    public class Message
    {
        public long MessageId { get; set; }
        public DateTime DispatchTime { get; set; }

        public long ChannelId { get; set; }
        public Channel Channel { get; set; }
    }
}
