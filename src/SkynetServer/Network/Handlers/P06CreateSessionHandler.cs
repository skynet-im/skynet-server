using Microsoft.EntityFrameworkCore;
using SkynetServer.Database.Entities;
using SkynetServer.Extensions;
using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using SkynetServer.Services;
using SkynetServer.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkynetServer.Network.Handlers
{
    internal class P06CreateSessionHandler : PacketHandler<P06CreateSession>
    {
        private readonly ConnectionsService connections;
        private readonly MessageInjectionService injector;

        public P06CreateSessionHandler(ConnectionsService connections, MessageInjectionService injector)
        {
            this.connections = connections;
            this.injector = injector;
        }

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

            Message deviceList = await injector.CreateDeviceList(Client.AccountId).ConfigureAwait(false);

            Client.Authenticate(confirmation.Account.AccountId, session.SessionId);

            response.StatusCode = CreateSessionStatus.Success;
            await Client.Send(response).ConfigureAwait(false);

            _ = await Delivery.SyncChannels(Client, new List<long>()).ConfigureAwait(false);
            _ = Delivery.SyncMessages(Client, lastMessageId: default);
            _ = Client.Enqueue(Packets.New<P0FSyncFinished>());

            _ = Delivery.SendMessage(deviceList, null);

            Client old = connections.Add(Client);
            if (old != null)
            {
                _ = old.DisposeAsync(true, false);
            }
        }
    }
}
