using Microsoft.EntityFrameworkCore;
using Skynet.Protocol.Model;
using Skynet.Protocol.Packets;
using Skynet.Server.Database.Entities;
using Skynet.Server.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Skynet.Server.Network.Handlers
{
    internal class P04DeleteAccountHandler : PacketHandler<P04DeleteAccount>
    {
        private readonly ConnectionsService connections;

        public P04DeleteAccountHandler(ConnectionsService connections)
        {
            this.connections = connections;
        }

        public override async ValueTask Handle(P04DeleteAccount packet)
        {
            var response = Packets.New<P05DeleteAccountResponse>();

            byte[] passwordHash;
            using (var csp = SHA256.Create())
                passwordHash = csp.ComputeHash(packet.KeyHash);

            // EF Core converts the C# == operator to = in SQL which compares the contents of byte arrays
            Account account = await Database.Accounts.AsTracking()
                .SingleOrDefaultAsync(a => a.AccountId == Client.AccountId && a.PasswordHash == packet.KeyHash).ConfigureAwait(false);
            if (account == null)
            {
                response.StatusCode = DeleteAccountStatus.InvalidCredentials;
                await Client.Send(response).ConfigureAwait(false);
                return;
            }

            account.DeletionTime = DateTime.Now;
            try
            {
                await Database.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException) // Account is being deleted by a another handler
            {
                // We can safely send a response to the client because the second handler is waiting for this one to return
                response.StatusCode = DeleteAccountStatus.Success;
                await Client.Send(response).ConfigureAwait(false);
                await Client.DisposeAsync(waitForHandling: false).ConfigureAwait(false);
            }


            // Delete mail confirmations and sessions
            // This will prevent clients from logging in with the deleted account
            MailConfirmation[] mailConfirmations = await Database.MailConfirmations.AsQueryable()
                .Where(c => c.AccountId == Client.AccountId)
                .ToArrayAsync().ConfigureAwait(false);
            Session[] sessions = await Database.Sessions.AsQueryable()
                .Where(s => s.AccountId == Client.AccountId)
                .ToArrayAsync().ConfigureAwait(false);
            Database.MailConfirmations.RemoveRange(mailConfirmations);
            Database.Sessions.RemoveRange(sessions);
            await Database.SaveChangesAsync().ConfigureAwait(false);


            // Kick all sessions
            var tasks = new List<Task>();
            foreach (Session session in sessions)
            {
                if (connections.TryGet(session.SessionId, out IClient client) && !ReferenceEquals(client, Client))
                {
                    tasks.Add(client.DisposeAsync(true, true, false).AsTask());
                }
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);


            // Do all remaining database operations
            ChannelMember[] memberships = await Database.ChannelMembers.AsQueryable()
                .Where(m => m.AccountId == Client.AccountId)
                .ToArrayAsync().ConfigureAwait(false);
            Database.ChannelMembers.RemoveRange(memberships);
            await Database.SaveChangesAsync().ConfigureAwait(false);


            // Finish handling and close last connection
            response.StatusCode = DeleteAccountStatus.Success;
            await Client.Send(response).ConfigureAwait(false);
            await Client.DisposeAsync(waitForHandling: false).ConfigureAwait(false);
        }
    }
}
