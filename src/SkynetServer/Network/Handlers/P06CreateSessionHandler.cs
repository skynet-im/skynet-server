using Microsoft.EntityFrameworkCore;
using SkynetServer.Database.Entities;
using SkynetServer.Extensions;
using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using SkynetServer.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkynetServer.Network.Handlers
{
    internal class P06CreateSessionHandler : PacketHandler<P06CreateSession>
    {
        public override async ValueTask Handle(P06CreateSession packet)
        {
            packet.AccountName = MailUtilities.SimplifyAddress(packet.AccountName);
            var response = Packets.New<P07CreateSessionResponse>();

            var confirmation = await Database.MailConfirmations.Include(c => c.Account)
                .SingleOrDefaultAsync(c => c.MailAddress == packet.AccountName).ConfigureAwait(false);
            if (confirmation == null)
            {
                response.StatusCode = CreateSessionStatus.InvalidCredentials;
                await Client.Send(response).ConfigureAwait(false);
                return;
            }
            if (confirmation.ConfirmationTime == default)
            {
                response.StatusCode = CreateSessionStatus.UnconfirmedAccount;
                await Client.Send(response).ConfigureAwait(false);
                return;
            }
            if (!packet.KeyHash.SequenceEqual(confirmation.Account.KeyHash))
            {
                response.StatusCode = CreateSessionStatus.InvalidCredentials;
                await Client.Send(response).ConfigureAwait(false);
                return;
            }

            Session session = await Database.AddSession(new Session
            {
                AccountId = confirmation.AccountId,
                ApplicationIdentifier = Client.ApplicationIdentifier,
                LastConnected = DateTime.Now,
                LastVersionCode = Client.VersionCode,
                FcmToken = packet.FcmRegistrationToken
            }).ConfigureAwait(false);

            Client.Authenticate(confirmation.Account.AccountId, session.SessionId);

            response.StatusCode = CreateSessionStatus.Success;
            await Client.Send(response).ConfigureAwait(false);

            // TODO: Change the following code not to be awaited anymore
            await Task.WhenAll(await Delivery.SyncChannels(Client, new List<long>()).ConfigureAwait(false)).ConfigureAwait(false);
            await Delivery.SyncMessages(Client, lastMessageId: default).ConfigureAwait(false);
            await Client.Send(Packets.New<P0FSyncFinished>()).ConfigureAwait(false);
        }
    }
}
