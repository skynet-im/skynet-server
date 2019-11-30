using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using SkynetServer.Database.Entities;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Wiry.Base32;

namespace SkynetServer.Database
{
    partial class DatabaseContext
    {
        public static long RandomId()
        {
            using var random = RandomNumberGenerator.Create();
            long result;
            do
            {
                Span<byte> value = stackalloc byte[8];
                random.GetBytes(value);
                result = BitConverter.ToInt64(value);

            } while (result == 0);
            return result;
        }

        public static string RandomToken()
        {
            using var random = RandomNumberGenerator.Create();
            byte[] value = new byte[10];
            random.GetBytes(value);
            return Base32Encoding.Standard.GetString(value).ToLowerInvariant();
        }

        public async Task<(Account, MailConfirmation, bool)> AddAccount(string mailAddress, byte[] keyHash)
        {
            Account account = new Account { KeyHash = keyHash };
            MailConfirmation confirmation = new MailConfirmation { Account = account, MailAddress = mailAddress };

            bool saved = false;
            do
            {
                try
                {
                    long id = RandomId();
                    string token = RandomToken();
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
                    long id = RandomId();
                    session.SessionId = id;
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
                    long id = RandomId();
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
    }
}
