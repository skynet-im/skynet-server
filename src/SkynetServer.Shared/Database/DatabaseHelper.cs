using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using SkynetServer.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Wiry.Base32;

namespace SkynetServer.Database
{
    public static class DatabaseHelper
    {
        public static async Task<Account> AddAccount(Account account)
        {
            using (DatabaseContext ctx = new DatabaseContext())
            {
                bool saved = false;
                do
                {
                    try
                    {
                        long id = RandomId();
                        account.AccountId = id;
                        ctx.Accounts.Add(account);
                        await ctx.SaveChangesAsync();
                        saved = true;
                    }
                    catch (DbUpdateException ex) when (ex?.InnerException is MySqlException mex && mex.Number == 1062)
                    {
                        // TODO: Throw if unique constraint violation is caused by AccountName
                    }
                } while (!saved);
                return account;
            }
        }

        public static async Task<Session> AddSession(Session session)
        {
            using (DatabaseContext ctx = new DatabaseContext())
            {
                bool saved = false;
                do
                {
                    try
                    {
                        long id = RandomId();
                        session.SessionId = id;
                        ctx.Sessions.Add(session);
                        await ctx.SaveChangesAsync();
                        saved = true;
                    }
                    catch (DbUpdateException ex) when (ex?.InnerException is MySqlException mex && mex.Number == 1062)
                    {
                    }
                } while (!saved);
                return session;
            }
        }

        public static async Task<Channel> AddChannel(Channel channel)
        {
            using (DatabaseContext ctx = new DatabaseContext())
            {
                bool saved = false;
                do
                {
                    try
                    {
                        long id = RandomId();
                        channel.ChannelId = id;
                        ctx.Channels.Add(channel);
                        await ctx.SaveChangesAsync();
                        saved = true;
                    }
                    catch (DbUpdateException ex) when (ex?.InnerException is MySqlException mex && mex.Number == 1062)
                    {
                    }
                } while (!saved);
                return channel;
            }
        }

        private static long GetMessageId(long channelId)
        {
            // TODO: Making this method asynchronous leads to 20 times less performance in Benchmarks
            //       This is caused by a cheap implementation of AsyncParallel.ForAsync which starts all tasks at once
            //       If this issue is limited to benchmarks it is sufficient to change the AsyncParallel implementation
            //       Otherwise I would recommend to use an async Semaphore to limit the number of tasks running in parallel

            using (DatabaseContext ctx = new DatabaseContext())
            {
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
        }

        public static async Task<Message> AddMessage(Message message)
        {
            using (DatabaseContext ctx = new DatabaseContext())
            {
                message.MessageId = GetMessageId(message.ChannelId);
                ctx.Messages.Add(message);
                await ctx.SaveChangesAsync();
                return message;
            }
        }

        public static async Task<MailConfirmation> AddMailConfirmation(Account account, string address)
        {
            MailConfirmation confirmation = new MailConfirmation()
            {
                AccountId = account.AccountId,
                MailAddress = address,
                CreationTime = DateTime.Now
            };

            using (DatabaseContext ctx = new DatabaseContext())
            {
                bool saved = false;
                do
                {
                    try
                    {
                        string token = RandomToken();
                        confirmation.Token = token;
                        ctx.MailConfirmations.Add(confirmation);
                        await ctx.SaveChangesAsync();
                        saved = true;
                    }
                    catch (DbUpdateException ex) when (ex?.InnerException is MySqlException mex && mex.Number == 1062)
                    {
                        // TODO: Throw if unique constraint violation is caused by MailAddress
                    }

                } while (!saved);
                return confirmation;
            }
        }

        private static long RandomId()
        {
            using (var random = RandomNumberGenerator.Create())
            {
                long result;
                do
                {
                    Span<byte> value = stackalloc byte[8];
                    random.GetBytes(value);
                    result = BitConverter.ToInt64(value);

                } while (result == 0);
                return result;
            }
        }

        private static string RandomToken()
        {
            using (var random = RandomNumberGenerator.Create())
            {
                byte[] value = new byte[10];
                random.GetBytes(value);
                return Base32Encoding.Standard.GetString(value).ToLower();
            }
        }
    }
}
