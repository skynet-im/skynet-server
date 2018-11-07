using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
            account.HasKey(p => p.AccountId);
            account.HasAlternateKey(p => p.AccountName);

            var channel = modelBuilder.Entity<Channel>();
            channel.HasKey(p => p.ChannelId);
            channel.Property(p => p.ChannelId).ValueGeneratedNever();

            var message = modelBuilder.Entity<Message>();
            message.HasKey(p => new { p.ChannelId, p.MessageId });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=MyDatabase;Trusted_Connection=True;");
        }
    }
}
