using Microsoft.EntityFrameworkCore;
using Skynet.Protocol.Model;
using Skynet.Protocol.Packets;
using SkynetServer.Database.Entities;
using SkynetServer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkynetServer.Network.Handlers
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

            Account account = await Database.Accounts.AsTracking()
                .SingleOrDefaultAsync(a => a.AccountId == Client.AccountId && a.KeyHash == packet.KeyHash).ConfigureAwait(false);
            if (account == null)
            {
                response.StatusCode = DeleteAccountStatus.InvalidCredentials;
                _ = Client.Send(response);
                return;
            }

            // Delete mail confirmations and sessions
            account.DeletionTime = DateTime.Now;
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
            // TODO: Use a concurrency token on Account.DeletionTime to avoid concurrent account deletions which will lead to dead locks
            var tasks = new List<Task>();
            foreach (Session session in sessions)
            {
                if (connections.TryGet(session.SessionId, out IClient client) && !ReferenceEquals(client, Client))
                {
                    tasks.Add(client.DisposeAsync(true, false).AsTask());
                }
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);

            // Finish handling and close last connection
            response.StatusCode = DeleteAccountStatus.Success;
            await Client.Send(response).ConfigureAwait(false);
            await Client.DisposeAsync(false, true).ConfigureAwait(false);

            ChannelMember[] memberships = await Database.ChannelMembers.AsQueryable()
                .Where(m => m.AccountId == Client.AccountId)
                .ToArrayAsync().ConfigureAwait(false);
            Database.ChannelMembers.RemoveRange(memberships);
            await Database.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
