using Microsoft.EntityFrameworkCore;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using SkynetServer.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Handlers
{
    internal class P06CreateSessionHandler : PacketHandler<P06CreateSession>
    {
        public override async ValueTask Handle(P06CreateSession packet)
        {
            packet.AccountName = MailUtilities.SimplifyAddress(packet.AccountName);
            var response = Packet.New<P07CreateSessionResponse>();

            var confirmation = await Database.MailConfirmations.Include(c => c.Account)
                .SingleOrDefaultAsync(c => c.MailAddress == packet.AccountName);
            if (confirmation == null)
                response.StatusCode = CreateSessionStatus.InvalidCredentials;
            else if (confirmation.ConfirmationTime == default)
                response.StatusCode = CreateSessionStatus.UnconfirmedAccount;
            else if (new Span<byte>(packet.KeyHash).SequenceEqual(confirmation.Account.KeyHash))
            {
                Session session = await DatabaseHelper.AddSession(new Session
                {
                    AccountId = confirmation.AccountId,
                    ApplicationIdentifier = Client.ApplicationIdentifier,
                    LastConnected = DateTime.Now,
                    LastVersionCode = Client.VersionCode,
                    FcmToken = packet.FcmRegistrationToken
                });

                Client.Authenticate(confirmation.Account.AccountId, session.SessionId);

                response.StatusCode = CreateSessionStatus.Success;
                await Client.SendPacket(response);
                await SendMessages(new List<(long channelId, long messageId)>());
                return;
            }
            else
                response.StatusCode = CreateSessionStatus.InvalidCredentials;
            await Client.SendPacket(response);
        }
    }
}
