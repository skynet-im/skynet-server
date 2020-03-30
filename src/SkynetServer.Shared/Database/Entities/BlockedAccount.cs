﻿using System;
using System.Collections.Generic;

namespace SkynetServer.Database.Entities
{
    public class BlockedAccount
    {
        public long AccountId { get; set; }
        public Account Account { get; set; }

        public long OwnerId { get; set; }
        public Account Owner { get; set; }
    }
}
