using Microsoft.EntityFrameworkCore;
using Skynet.Protocol.Model;
using Skynet.Protocol.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skynet.Server.Network.Handlers
{
    internal sealed class P32DeviceListRequestHandler : PacketHandler<P32DeviceListRequest>
    {
        public override async ValueTask Handle(P32DeviceListRequest packet)
        {
            List<SessionDetails> sessionDetails = await Database.Sessions.AsQueryable()
                .Where(s => s.AccountId == Client.AccountId)
                .Select(s => new SessionDetails(s.SessionId, s.LastConnected, s.LastVersionCode))
                .ToListAsync().ConfigureAwait(false);

            var deviceListResponse = Packets.New<P33DeviceListResponse>();
            deviceListResponse.SessionDetails = sessionDetails;
            await Client.Send(deviceListResponse).ConfigureAwait(false);
        }
    }
}
