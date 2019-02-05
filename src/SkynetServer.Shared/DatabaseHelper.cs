using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using SkynetServer.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Wiry.Base32;

namespace SkynetServer
{
    public static class DatabaseHelper
    {
        public static readonly object MailConfirmationsLock = new object();

        public static Account AddAccount(Account account)
        {
            using (DatabaseContext ctx = new DatabaseContext())
            {
                bool saved = false;
                do
                {
                    try
                    {
                        long id = RandomId() & 0xf;
                        account.AccountId = id;
                        ctx.Accounts.Add(account);
                        ctx.SaveChanges();
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

        public static Session AddSession(Session session)
        {
            using (DatabaseContext ctx = new DatabaseContext())
            {
                bool saved = false;
                do
                {
                    try
                    {
                        long id = RandomId() & 0xf;
                        session.SessionId = id;
                        ctx.Sessions.Add(session);
                        ctx.SaveChanges();
                        saved = true;
                    }
                    catch (DbUpdateException ex) when (ex?.InnerException is MySqlException mex && mex.Number == 1062)
                    {

                    }
                } while (!saved);
                return session;
            }
        }

        public static Channel AddChannel(Channel channel)
        {
            using (DatabaseContext ctx = new DatabaseContext())
            {
                bool saved = false;
                do
                {
                    try
                    {
                        long id = RandomId() & 0xf;
                        channel.ChannelId = id;
                        ctx.Channels.Add(channel);
                        ctx.SaveChanges();
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

        public static Message AddMessage(Message message)
        {
            using (DatabaseContext ctx = new DatabaseContext())
            {
                message.MessageId = GetMessageId(message.ChannelId);
                ctx.Messages.Add(message);
                ctx.SaveChanges();
                return message;
            }
        }

        public static MailConfirmation AddMailConfirmation(Account account, string address)
        {
            using (DatabaseContext ctx = new DatabaseContext())
            {
                MailConfirmation confirmation = new MailConfirmation()
                {
                    AccountId = account.AccountId,
                    MailAddress = address,
                    CreationTime = DateTime.Now
                };

                lock (MailConfirmationsLock)
                {
                    string token;
                    do
                    {
                        token = RandomToken();
                    } while (ctx.MailConfirmations.Any(x => x.Token == token));
                    confirmation.Token = token;
                    ctx.MailConfirmations.Add(confirmation);
                    ctx.SaveChanges();
                }
                return confirmation;
            }
        }

        private static long RandomId()
        {
            using (var random = RandomNumberGenerator.Create())
            {
                Span<byte> value = stackalloc byte[8];
                random.GetBytes(value);
                return BitConverter.ToInt64(value);
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
