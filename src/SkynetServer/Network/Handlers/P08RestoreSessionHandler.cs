using Microsoft.EntityFrameworkCore;
using SkynetServer.Database.Entities;
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
            if (session == null || !new Span<byte>(packet.SessionToken).SequenceEqual(session.Account.KeyHash))
            {
                response.StatusCode = RestoreSessionStatus.InvalidSession;
                await Client.SendPacket(response).ConfigureAwait(false);
                return;
            }

            session.LastConnected = DateTime.Now;
            await Database.SaveChangesAsync().ConfigureAwait(false);

            Client.Authenticate(session.Account.AccountId, session.SessionId);

            response.StatusCode = RestoreSessionStatus.Success;
            await Client.SendPacket(response).ConfigureAwait(false);

            await SendMessages(packet.Channels);
        }
    }
}
