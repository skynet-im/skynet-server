using Microsoft.EntityFrameworkCore;
using Skynet.Protocol.Model;
using Skynet.Protocol.Packets;
using Skynet.Server.Database.Entities;
using Skynet.Server.Services;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Skynet.Server.Network.Handlers
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
            byte[] sessionTokenHash;
            using (var csp = SHA256.Create())
                sessionTokenHash = csp.ComputeHash(packet.SessionToken);

            // EF Core converts the C# == operator to = in SQL which compares the contents of byte arrays
            Session session = await Database.Sessions.AsTracking()
                .SingleOrDefaultAsync(s => s.SessionId == packet.SessionId && s.SessionTokenHash == sessionTokenHash).ConfigureAwait(false);
            var response = Packets.New<P09RestoreSessionResponse>();
            if (session == null)
            {
                response.StatusCode = RestoreSessionStatus.InvalidSession;
                await Client.Send(response).ConfigureAwait(false);
                return;
            }

            session.LastConnected = DateTime.Now;
            session.LastVersionCode = Client.VersionCode;
            await Database.SaveChangesAsync().ConfigureAwait(false);

            Client.Authenticate(session.AccountId, session.SessionId);

            response.StatusCode = RestoreSessionStatus.Success;
            await Client.Send(response).ConfigureAwait(false);

            await Delivery.StartSyncChannels(Client, packet.Channels, packet.LastMessageId).ConfigureAwait(false);

            IClient old = connections.Add(Client);
            if (old != null)
            {
                _ = old.DisposeAsync(unregister: false);
            }
        }
    }
}
