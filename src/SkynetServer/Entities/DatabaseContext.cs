using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SkynetServer.Entities
{
    public class DatabaseContext : DbContext
    {
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var account = modelBuilder.Entity<Account>();
            account.HasKey(a => a.AccountId);
            account.HasAlternateKey(a => a.AccountName);

            var channel = modelBuilder.Entity<Channel>();
            channel.HasKey(c => c.ChannelId);
            channel.Property(c => c.ChannelId).ValueGeneratedNever();

            var message = modelBuilder.Entity<Message>();
            message.HasKey(m => new { m.ChannelId, m.MessageId });
            message.HasOne(m => m.Channel).WithMany(c => c.Messages).HasForeignKey(m => m.ChannelId);
            message.Property(m => m.MessageId).ValueGeneratedNever();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=MyDatabase;Trusted_Connection=True;");
        }

        public void AddMessage(Message message)
        {
            long id = Messages
                .Where(m => m.ChannelId == message.ChannelId)
                .OrderByDescending(m => m.MessageId)
                .FirstOrDefault()?.MessageId ?? 0;

            Exception exception = null;

            for (int i = 0; i < 512; i++)
            {
                try
                {
                    message.MessageId = id;
                    Messages.Add(message);
                    SaveChanges();
                    return;
                }
                catch (Exception ex)
                {
                    exception = ex;
                    id++;
                }
            }

            throw exception;
        }
    }
}
