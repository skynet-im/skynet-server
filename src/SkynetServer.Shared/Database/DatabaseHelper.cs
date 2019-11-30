using Microsoft.EntityFrameworkCore;
using SkynetServer.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkynetServer.Database
{
    public static class DatabaseHelper
    {

        private static long GetMessageId(long channelId)
        {
            // TODO: Making this method asynchronous leads to 20 times less performance in Benchmarks
            //       This is caused by a cheap implementation of AsyncParallel.ForAsync which starts all tasks at once
            //       If this issue is limited to benchmarks it is sufficient to change the AsyncParallel implementation
            //       Otherwise I would recommend to use an async Semaphore to limit the number of tasks running in parallel

            using DatabaseContext ctx = new DatabaseContext();
            bool saved = false;
            Channel channel = ctx.Channels.Single(c => c.ChannelId == channelId);
            long messageId = ++channel.MessageIdCounter;
            do
            {
                try
                {
                    ctx.SaveChanges();
                    saved = true;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    var entry = ex.Entries.Single();
                    var proposedValues = entry.CurrentValues;
                    var databaseValues = entry.GetDatabaseValues();
                    const string name = nameof(Channel.MessageIdCounter);
                    proposedValues[name] = messageId = (long)databaseValues[name] + 1;
                    entry.OriginalValues.SetValues(databaseValues);
                }

            } while (!saved);
            return messageId;
        }

        public static async Task<Message> AddMessage(Message message)
        {
            using DatabaseContext ctx = new DatabaseContext();
            message.MessageId = GetMessageId(message.ChannelId);
            ctx.Messages.Add(message);
            await ctx.SaveChangesAsync();
            return message;
        }

        public static async Task<Message> AddMessage(Message message, List<MessageDependency> dependencies)
        {
            using (DatabaseContext ctx = new DatabaseContext())
            {
                message.MessageId = GetMessageId(message.ChannelId);
                ctx.Messages.Add(message);

                foreach (MessageDependency dependency in dependencies)
                {
                    dependency.OwningChannelId = message.ChannelId;
                    dependency.OwningMessageId = message.MessageId;
                }

                ctx.MessageDependencies.AddRange(dependencies);
                await ctx.SaveChangesAsync();
            }

            message.Dependencies = dependencies;
            return message;
        }
    }
}
