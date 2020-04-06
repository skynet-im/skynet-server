using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Skynet.Server.Database.Entities;
using Skynet.Server.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skynet.Server.Database
{
    public partial class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<ChannelMember> ChannelMembers { get; set; }
        public DbSet<BlockedAccount> BlockedAccounts { get; set; }
        public DbSet<BlockedConversation> BlockedConversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageDependency> MessageDependencies { get; set; }
        public DbSet<MailConfirmation> MailConfirmations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var account = modelBuilder.Entity<Account>();
            account.HasKey(a => a.AccountId);
            account.Property(a => a.AccountId).ValueGeneratedNever();
            account.Property(a => a.KeyHash).IsRequired();
            account.Property(a => a.CreationTime).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            var session = modelBuilder.Entity<Session>();
            session.HasKey(s => s.SessionId);
            session.HasAlternateKey(s => s.WebToken);
            session.Property(s => s.CreationTime).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            session.Property(s => s.SessionId).ValueGeneratedNever();
            session.Property(s => s.SessionToken).IsRequired();
            session.Property(s => s.ApplicationIdentifier).IsRequired();
            session.HasOne(s => s.Account).WithMany(a => a.Sessions).HasForeignKey(s => s.AccountId);

            var channel = modelBuilder.Entity<Channel>();
            channel.HasKey(c => c.ChannelId);
            channel.Property(c => c.ChannelId).ValueGeneratedNever();
            channel.Property(c => c.ChannelType).HasConversion<byte>();
            channel.Property(c => c.CreationTime).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            channel.HasOne(c => c.Owner).WithMany(a => a.OwnedChannels).HasForeignKey(c => c.OwnerId);

            var channelMember = modelBuilder.Entity<ChannelMember>();
            channelMember.HasKey(m => new { m.ChannelId, m.AccountId });
            channelMember.HasOne(m => m.Channel).WithMany(c => c.ChannelMembers).HasForeignKey(m => m.ChannelId);
            channelMember.HasOne(m => m.Account).WithMany(a => a.ChannelMemberships).HasForeignKey(m => m.AccountId);

            var blockedAccount = modelBuilder.Entity<BlockedAccount>();
            blockedAccount.HasKey(b => new { b.OwnerId, b.AccountId });
            blockedAccount.HasOne(b => b.Owner).WithMany(a => a.BlockedAccounts).HasForeignKey(b => b.OwnerId);
            blockedAccount.HasOne(b => b.Account).WithMany(a => a.Blockers).HasForeignKey(b => b.AccountId);

            var blockedConv = modelBuilder.Entity<BlockedConversation>();
            blockedConv.HasKey(b => new { b.OwnerId, b.ChannelId });
            blockedConv.HasOne(b => b.Owner).WithMany(a => a.BlockedConversations).HasForeignKey(b => b.OwnerId);
            blockedConv.HasOne(b => b.Channel).WithMany(c => c.Blockers).HasForeignKey(b => b.ChannelId);

            var message = modelBuilder.Entity<Message>();
            message.HasKey(m => m.MessageId);
            message.HasOne(m => m.Channel).WithMany(c => c.Messages).HasForeignKey(m => m.ChannelId);
            message.Property(m => m.MessageId).ValueGeneratedOnAdd();
            message.Property(m => m.DispatchTime).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            message.Property(m => m.MessageFlags).HasConversion<byte>();

            var messageDependency = modelBuilder.Entity<MessageDependency>();
            messageDependency.HasKey(d => d.AutoId);
            messageDependency.HasOne(d => d.OwningMessage).WithMany(m => m.Dependencies).HasForeignKey(d => d.OwningMessageId);
            messageDependency.HasOne(d => d.Message).WithMany(m => m.Dependants).HasForeignKey(d => d.MessageId);
            messageDependency.Property(d => d.AutoId).ValueGeneratedOnAdd();

            var mailConfirmation = modelBuilder.Entity<MailConfirmation>();
            mailConfirmation.HasKey(c => c.MailAddress);
            mailConfirmation.HasAlternateKey(c => c.Token);
            mailConfirmation.Property(c => c.CreationTime).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            mailConfirmation.HasOne(c => c.Account).WithMany(a => a.MailConfirmations).HasForeignKey(c => c.AccountId);
        }

        #region insertion helpers
        public async Task<(Account, MailConfirmation, bool)> AddAccount(string mailAddress, byte[] keyHash)
        {
            Account account = new Account { KeyHash = keyHash };
            MailConfirmation confirmation = new MailConfirmation { Account = account, MailAddress = mailAddress };

            bool saved = false;
            do
            {
                try
                {
                    long id = SkynetRandom.Id();
                    string token = SkynetRandom.String(10);
                    account.AccountId = id;
                    confirmation.Token = token;
                    Accounts.Add(account);
                    MailConfirmations.Add(confirmation);
                    await SaveChangesAsync().ConfigureAwait(false);
                    saved = true;
                }
                catch (DbUpdateException ex) when (ex?.InnerException is MySqlException mex && mex.Number == 1062)
                {
                    // Return false if unique constraint violation is caused by the mail address
                    // An example for mex.Message is "Duplicate entry 'concurrency@unit.test' for key 'PRIMARY'"

                    if (mex.Message.Contains('@', StringComparison.Ordinal))
                        return (null, null, false);
                }
            } while (!saved);
            return (account, confirmation, true);
        }

        public async Task<Session> AddSession(Session session)
        {
            bool saved = false;
            do
            {
                try
                {
                    long id = SkynetRandom.Id();
                    session.SessionId = id;
                    session.SessionToken = SkynetRandom.Bytes(32);
                    session.WebToken = SkynetRandom.String(30);
                    Sessions.Add(session);
                    await SaveChangesAsync().ConfigureAwait(false);
                    saved = true;
                }
                catch (DbUpdateException ex) when (ex?.InnerException is MySqlException mex && mex.Number == 1062)
                {
                }
            } while (!saved);
            return session;
        }

        public async Task<Channel> AddChannel(Channel channel, params ChannelMember[] members)
        {
            bool saved = false;
            do
            {
                try
                {
                    long id = SkynetRandom.Id();
                    channel.ChannelId = id;
                    Channels.Add(channel);

                    foreach (ChannelMember member in members)
                    {
                        member.ChannelId = id;
                    }

                    ChannelMembers.AddRange(members);

                    await SaveChangesAsync().ConfigureAwait(false);
                    saved = true;
                }
                catch (DbUpdateException ex) when (ex?.InnerException is MySqlException mex && mex.Number == 1062)
                {
                }
            } while (!saved);
            return channel;
        }
#endregion
    }
}
