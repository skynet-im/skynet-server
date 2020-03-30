using System;
using System.Collections.Generic;

namespace SkynetServer.Database.Entities
{
    public class MailConfirmation
    {
        public string MailAddress { get; set; }
        public string Token { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime ConfirmationTime { get; set; }

        public long AccountId { get; set; }
        public Account Account { get; set; }
    }
}
