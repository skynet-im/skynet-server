using Microsoft.EntityFrameworkCore;
using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkynetServer.Network.Handlers
{
    internal class P2DSearchAccountHandler : PacketHandler<P2DSearchAccount>
    {
        public override async ValueTask Handle(P2DSearchAccount packet)
        {
            var results = await Database.MailConfirmations.AsQueryable()
                .Where(c => c.AccountId != Client.AccountId
                    && c.MailAddress.Contains(packet.Query, StringComparison.Ordinal)
                    && c.ConfirmationTime != default) // Exclude unconfirmed accounts
                .Take(100).ToListAsync().ConfigureAwait(false); // Limit to 100 entries
            var response = Packets.New<P2ESearchAccountResponse>();
            foreach (var result in results)
                response.Results.Add(new SearchResult(result.AccountId, result.MailAddress));
            // Forward public packets to fully implement the Skynet protocol v5
            await Client.Send(response).ConfigureAwait(false);
        }
    }
}
