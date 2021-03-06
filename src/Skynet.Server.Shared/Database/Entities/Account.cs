﻿using System;
using System.Collections.Generic;

namespace Skynet.Server.Database.Entities
{
    public class Account
    {
        public long AccountId { get; set; }
        public byte[] PasswordHash { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime DeletionTime { get; set; }

        public IEnumerable<Session> Sessions { get; set; }
        public IEnumerable<BlockedAccount> BlockedAccounts { get; set; }
        public IEnumerable<BlockedConversation> BlockedConversations { get; set; }
        public IEnumerable<BlockedAccount> Blockers { get; set; }
        public IEnumerable<Channel> OwnedChannels { get; set; }
        public IEnumerable<ChannelMember> ChannelMemberships { get; set; }
        public IEnumerable<MailConfirmation> MailConfirmations { get; set; }
    }
}
