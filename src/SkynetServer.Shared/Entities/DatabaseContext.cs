using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkynetServer.Entities
{
    public class DatabaseContext : DbContext
    {
        public static readonly object AccountsLock = new object();
        public static readonly object ChannelsLock = new object();
        public static readonly object MessagesLock = new object();

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageDependency> MessageDependencies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var account = modelBuilder.Entity<Account>();
            account.HasKey(a => a.AccountId);
            account.HasAlternateKey(a => a.AccountName);

            var session = modelBuilder.Entity<Session>();
            session.HasKey(s => new { s.AccountId, s.SessionId });
            session.HasOne(s => s.Account).WithMany(a => a.Sessions).HasForeignKey(s => s.AccountId);

            var blockedAccount = modelBuilder.Entity<BlockedAccount>();
            blockedAccount.HasOne(b => b.Owner).WithMany(a => a.BlockedAccounts).HasForeignKey(b => b.OwnerId);
            blockedAccount.HasOne(b => b.Account).WithMany(a => a.Blockers).HasForeignKey(b => b.AccountId);

            var blockedConv = modelBuilder.Entity<BlockedConversation>();
            blockedConv.HasOne(b => b.Owner).WithMany(a => a.BlockedConversations).HasForeignKey(b => b.OwnerId);
            blockedConv.HasOne(b => b.Channel).WithMany(c => c.Blockers).HasForeignKey(b => b.ChannelId);

            var channel = modelBuilder.Entity<Channel>();
            channel.HasKey(c => c.ChannelId);
            channel.Property(c => c.ChannelId).ValueGeneratedNever();
            channel.Property(c => c.ChannelType).HasConversion<byte>();
            // TODO: Foreign Key OwnerId
            // TODO: Optional Foreign Key OtherId

            var groupMember = modelBuilder.Entity<GroupMember>();
            groupMember.HasKey(m => new { m.ChannelId, m.AccountId });
            // TODO: Foreign Key ChannelId
            // TODO: Foreign Key AccountId

            var message = modelBuilder.Entity<Message>();
            message.HasKey(m => new { m.ChannelId, m.MessageId });
            message.HasOne(m => m.Channel).WithMany(c => c.Messages).HasForeignKey(m => m.ChannelId);
            message.Property(m => m.MessageId).ValueGeneratedNever();
            message.Property(m => m.MessageFlags).HasConversion<byte>();

            var messageDependency = modelBuilder.Entity<MessageDependency>();
            messageDependency.HasKey(d => new { d.AccountId, d.ChannelId, d.MessageId });
            messageDependency.HasOne(d => d.Message).WithMany(m => m.Dependencies).HasForeignKey(d => new { d.ChannelId, d.MessageId });

            var mailAddressConfirmation = modelBuilder.Entity<MailAddressConfirmation>();
            mailAddressConfirmation.HasKey(c => c.MailAddress);
            mailAddressConfirmation.HasAlternateKey(c => c.Token);
            // TODO: Foreign Key AccountId
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseLoggerFactory(new LoggerFactory(new[] { new ConsoleLoggerProvider((category, level) => level >= LogLevel.Information, false) }));
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.UseMySql("server=localhost;Port=3306;Database=Skynet;UID=root");
        }


        public Account AddAccount(Account account)
        {
            lock (AccountsLock)
            {
                long id;
                do
                {
                    id = RandomId();
                } while (Accounts.Any(x => x.AccountId == id));
                account.AccountId = id;
                Accounts.Add(account);
                SaveChanges();
            }
            return account;
        }

        public Channel AddChannel(Channel channel)
        {
            lock (ChannelsLock)
            {
                long id;
                do
                {
                    id = RandomId();
                } while (Channels.Any(x => x.ChannelId == id));
                channel.ChannelId = id;
                Channels.Add(channel);
                SaveChanges();
            }
            return channel;
        }

        public void AddMessage(Message message)
        {
            /*Database.ExecuteSqlCommand($@"BEGIN;
SELECT @id := IFNULL(MAX(MessageId), 0) + 1 FROM Messages WHERE ChannelId = {message.ChannelId} FOR UPDATE;
INSERT INTO Messages (MessageId, DispatchTime, ChannelId) 
VALUES (@id, {message.DispatchTime}, {message.ChannelId});
COMMIT;");*/
            lock (MessagesLock)
            {
                Messages.Add(message);
                SaveChanges();
            }
        }

        private long RandomId()
        {
            Random random = new Random();
            Span<byte> value = stackalloc byte[8];
            random.NextBytes(value);
            return BitConverter.ToInt64(value);
        }
    }
}
