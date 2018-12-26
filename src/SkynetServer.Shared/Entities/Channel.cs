using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Entities
{
    public class Channel
    {
        public long ChannelId { get; set; }

        public IEnumerable<Message> Messages { get; set; }
    }
}
