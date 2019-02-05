using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using SkynetServer.Entities;
using System;
using System.Collections.Generic;

namespace SkynetServer
{
    public class DatabaseContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageDependency> MessageDependencies { get; set; }
        public DbSet<MailConfirmation> MailConfirmations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var account = modelBuilder.Entity<Account>();
            account.HasKey(a => a.AccountId);
            account.HasAlternateKey(a => a.AccountName);
            account.Property(a => a.AccountId).ValueGeneratedNever();
            account.Property(a => a.KeyHash).IsRequired();

            var session = modelBuilder.Entity<Session>();
            session.HasKey(s => new { s.AccountId, s.SessionId });
            session.HasOne(s => s.Account).WithMany(a => a.Sessions).HasForeignKey(s => s.AccountId);
            session.Property(s => s.SessionId).ValueGeneratedNever();
            session.Property(s => s.ApplicationIdentifier).IsRequired();

            var blockedAccount = modelBuilder.Entity<BlockedAccount>();
            blockedAccount.HasKey(b => new { b.OwnerId, b.AccountId });
            blockedAccount.HasOne(b => b.Owner).WithMany(a => a.BlockedAccounts).HasForeignKey(b => b.OwnerId);
            blockedAccount.HasOne(b => b.Account).WithMany(a => a.Blockers).HasForeignKey(b => b.AccountId);

            var blockedConv = modelBuilder.Entity<BlockedConversation>();
            blockedConv.HasKey(b => new { b.OwnerId, b.ChannelId });
            blockedConv.HasOne(b => b.Owner).WithMany(a => a.BlockedConversations).HasForeignKey(b => b.OwnerId);
            blockedConv.HasOne(b => b.Channel).WithMany(c => c.Blockers).HasForeignKey(b => b.ChannelId);

            var channel = modelBuilder.Entity<Channel>();
            channel.HasKey(c => c.ChannelId);
            channel.Property(c => c.ChannelId).ValueGeneratedNever();
            channel.Property(c => c.ChannelType).HasConversion<byte>();
            channel.Property(c => c.MessageIdCounter).IsConcurrencyToken();
            channel.HasOne(c => c.Owner).WithMany(a => a.OwnedChannels).HasForeignKey(c => c.OwnerId);
            channel.HasOne(c => c.Other).WithMany(a => a.OtherChannels).HasForeignKey(c => c.OtherId);

            var groupMember = modelBuilder.Entity<GroupMember>();
            groupMember.HasKey(m => new { m.ChannelId, m.AccountId });
            groupMember.HasOne(m => m.Channel).WithMany(c => c.GroupMembers).HasForeignKey(m => m.ChannelId);
            groupMember.HasOne(m => m.Account).WithMany(a => a.GroupMemberships).HasForeignKey(m => m.AccountId);

            var message = modelBuilder.Entity<Message>();
            message.HasKey(m => new { m.ChannelId, m.MessageId });
            message.HasOne(m => m.Channel).WithMany(c => c.Messages).HasForeignKey(m => m.ChannelId);
            message.Property(m => m.MessageId).ValueGeneratedNever();
            message.Property(m => m.MessageFlags).HasConversion<byte>();

            var messageDependency = modelBuilder.Entity<MessageDependency>();
            messageDependency.HasKey(d => new { d.AccountId, d.ChannelId, d.MessageId });
            messageDependency.HasOne(d => d.Message).WithMany(m => m.Dependencies).HasForeignKey(d => new { d.ChannelId, d.MessageId });

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
