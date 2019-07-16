﻿using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Database.Entities
{
    public class MessageDependency
    {
        public long OwningChannelId { get; set; }
        public long OwningMessageId { get; set; }
        public Message OwningMessage { get; set; }

        public long ChannelId { get; set; }
        public long MessageId { get; set; }
        public Message Message { get; set; }

        public long? AccountId { get; set; }
        public Account Account { get; set; }

        public int AutoId { get; set; }
    }
}