using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Entities
{
    public class Account
    {
        public long AccountId { get; set; }
        public string AccountName { get; set; }

        public IEnumerable<Session> Sessions { get; set; }
    }
}
