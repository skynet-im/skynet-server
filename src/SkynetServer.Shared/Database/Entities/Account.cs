using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Database.Entities
{
    public class Account
    {
        public long AccountId { get; set; }
        public string AccountName { get; set; }
        public byte[] KeyHash { get; set; }

        public IEnumerable<Session> Sessions { get; set; }
        public IEnumerable<BlockedAccount> BlockedAccounts { get; set; }
        public IEnumerable<BlockedConversation> BlockedConversations { get; set; }
        public IEnumerable<BlockedAccount> Blockers { get; set; }
        public IEnumerable<Channel> OwnedChannels { get; set; }
        public IEnumerable<Channel> OtherChannels { get; set; }
        public IEnumerable<GroupMember> GroupMemberships { get; set; }
        public IEnumerable<MailConfirmation> MailConfirmations { get; set; }
    }
}
