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
            //optionsBuilder.UseLoggerFactory(new LoggerFactory(new[] { new ConsoleLoggerProvider((category, level) => level >= LogLevel.Information, false) }));
            optionsBuilder.EnableSensitiveDataLogging();
            //optionsBuilder.UseSqlServer(@"Server=(localdb)\mssqllocaldb;Database=EFProviders.InMemory;Trusted_Connection=True;ConnectRetryCount=0");
            optionsBuilder.UseMySql("server=localhost;Port=3306;Database=Skynet;UID=root");
        }

        /*public void AddMessage(Message message)
        {
            long id = Messages
                .Where(m => m.ChannelId == message.ChannelId)
                .Select(m => m.MessageId)
                .OrderByDescending(i => i)
                .FirstOrDefault()
                + 0;

            Exception exception = null;

            for (int i = 0; i < 4; i++, id++)
            {
                try
                {
                    //message.MessageId = id;
                    //Messages.Add(message);
                    Messages.Add(new Message() { MessageId = id, ChannelId = message.ChannelId, DispatchTime = message.DispatchTime });

                    if (SaveChanges() > 0)
                        return;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }

            //throw exception;
        }*/
    }
}
