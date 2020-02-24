using Microsoft.EntityFrameworkCore;
using SkynetServer.Database.Entities;
using SkynetServer.Extensions;
using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using SkynetServer.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkynetServer.Network.Handlers
{
    internal class P08RestoreSessionHandler : PacketHandler<P08RestoreSession>
    {
        private readonly ConnectionsService connections;

        public P08RestoreSessionHandler(ConnectionsService connections)
        {
            this.connections = connections;
        }

        public override async ValueTask Handle(P08RestoreSession packet)
        {
            Session session = await Database.Sessions.Include(s => s.Account)
                .SingleOrDefaultAsync(s => s.SessionId == packet.SessionId).ConfigureAwait(false);
            var response = Packets.New<P09RestoreSessionResponse>();
            if (session == null || !packet.SessionToken.SequenceEqual(session.Account.KeyHash))
            {
                response.StatusCode = RestoreSessionStatus.InvalidSession;
                await Client.Send(response).ConfigureAwait(false);
                return;
            }

            session.LastConnected = DateTime.Now;
            await Database.SaveChangesAsync().ConfigureAwait(false);

            Client.Authenticate(session.Account.AccountId, session.SessionId);

            response.StatusCode = RestoreSessionStatus.Success;
            await Client.Send(response).ConfigureAwait(false);

            // TODO: Change the following code not to be awaited anymore
            await Task.WhenAll(await Delivery.SyncChannels(Client, packet.Channels).ConfigureAwait(false)).ConfigureAwait(false);
            await Delivery.SyncMessages(Client, packet.LastMessageId).ConfigureAwait(false);
            await Client.Send(Packets.New<P0FSyncFinished>()).ConfigureAwait(false);
        }
    }
}
