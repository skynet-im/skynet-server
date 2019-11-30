using Microsoft.EntityFrameworkCore;
using SkynetServer.Database.Entities;
using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Handlers
{
    internal class P08RestoreSessionHandler : PacketHandler<P08RestoreSession>
    {
        public override async ValueTask Handle(P08RestoreSession packet)
        {
            Session session = await Database.Sessions.Include(s => s.Account).SingleOrDefaultAsync(s => s.SessionId == packet.SessionId);
            var response = Packet.New<P09RestoreSessionResponse>();
            if (session != null && new Span<byte>(packet.SessionToken).SequenceEqual(session.Account.KeyHash))
            {
                session.LastConnected = DateTime.Now;
                await Database.SaveChangesAsync();
                Client.Authenticate(session.Account.AccountId, session.SessionId);
                response.StatusCode = RestoreSessionStatus.Success;
                await Client.SendPacket(response);
                await SendMessages(packet.Channels);
                return;
            }
            else
                response.StatusCode = RestoreSessionStatus.InvalidSession;
            await Client.SendPacket(response);
        }
    }
}
