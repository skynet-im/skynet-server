using Microsoft.EntityFrameworkCore;
using Skynet.Protocol.Model;
using Skynet.Protocol.Packets;
using Skynet.Server.Database.Entities;
using Skynet.Server.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skynet.Server.Network.Handlers
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
            var response = Packets.New<P07CreateSessionResponse>();

            // As of RFC 5321 the local-part of an email address should not be case-sensitive.
            var confirmation = await Database.MailConfirmations.Include(c => c.Account)
                .SingleOrDefaultAsync(c => c.MailAddress == packet.AccountName.ToLowerInvariant()).ConfigureAwait(false);
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
            if (!new Span<byte>(packet.KeyHash).SequenceEqual(confirmation.Account.KeyHash))
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

            _ = await Delivery.SyncChannels(Client, new List<long>(), lastMessageId: default).ConfigureAwait(false);
            _ = await Delivery.SendMessage(deviceList, null).ConfigureAwait(false);

            IClient old = connections.Add(Client);
            if (old != null)
            {
                _ = old.DisposeAsync(true, false);
            }
        }
    }
}
