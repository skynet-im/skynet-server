using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using SkynetServer.Database.Entities;
using System;
using System.Collections.Generic;

namespace SkynetServer.Database
{
    public class DatabaseContext : DbContext
    {
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

            var session = modelBuilder.Entity<Session>();
            session.HasKey(s => new { s.AccountId, s.SessionId });
            session.HasOne(s => s.Account).WithMany(a => a.Sessions).HasForeignKey(s => s.AccountId);
            session.Property(s => s.SessionId).ValueGeneratedNever();
            session.Property(s => s.ApplicationIdentifier).IsRequired();

            var channel = modelBuilder.Entity<Channel>();
            channel.HasKey(c => c.ChannelId);
            channel.Property(c => c.ChannelId).ValueGeneratedNever();
            channel.Property(c => c.ChannelType).HasConversion<byte>();
            channel.Property(c => c.MessageIdCounter).IsConcurrencyToken();
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
            message.HasKey(m => new { m.ChannelId, m.MessageId });
            message.HasOne(m => m.Channel).WithMany(c => c.Messages).HasForeignKey(m => m.ChannelId);
            message.Property(m => m.MessageId).ValueGeneratedNever();
            message.Property(m => m.MessageFlags).HasConversion<byte>();

            var messageDependency = modelBuilder.Entity<MessageDependency>();
            messageDependency.HasKey(d => new { d.OwningChannelId, d.OwningMessageId, d.ChannelId, d.MessageId, d.AccountId });
            messageDependency.HasOne(d => d.OwningMessage).WithMany(m => m.Dependencies).HasForeignKey(d => new { d.OwningChannelId, d.OwningMessageId });
            messageDependency.HasOne(d => d.Message).WithMany(m => m.Dependants).HasForeignKey(d => new { d.ChannelId, d.MessageId });

            var mailConfirmation = modelBuilder.Entity<MailConfirmation>();
            mailConfirmation.HasKey(c => c.MailAddress);
            mailConfirmation.HasAlternateKey(c => c.Token);
            mailConfirmation.HasOne(c => c.Account).WithMany(a => a.MailConfirmations).HasForeignKey(c => c.AccountId);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //optionsBuilder.UseLoggerFactory(new LoggerFactory(new[] { new ConsoleLoggerProvider((category, level) => level >= LogLevel.Information, false) }));
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.UseMySql("server=localhost;Port=3306;Database=Skynet;UID=root");
        }
    }
}
