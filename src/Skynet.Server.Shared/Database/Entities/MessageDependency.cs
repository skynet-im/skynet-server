﻿using System;
using System.Collections.Generic;

namespace Skynet.Server.Database.Entities
{
    public class MessageDependency
    {
        public long AutoId { get; set; }

        public long OwningMessageId { get; set; }
        public Message OwningMessage { get; set; }

        public long MessageId { get; set; }
        public Message Message { get; set; }

        public long? AccountId { get; set; }
        public Account Account { get; set; }
    }
}
