﻿using Microsoft.EntityFrameworkCore;
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
                response.SessionToken = new byte[32];
                response.WebToken = string.Empty;
                await Client.Send(response).ConfigureAwait(false);
                return;
            }
            if (confirmation.ConfirmationTime == default)
            {
                response.StatusCode = CreateSessionStatus.UnconfirmedAccount;
                response.SessionToken = new byte[32];
                response.WebToken = string.Empty;
                await Client.Send(response).ConfigureAwait(false);
                return;
            }
            if (!packet.KeyHash.SequenceEqual(confirmation.Account.KeyHash))
            {
                response.StatusCode = CreateSessionStatus.InvalidCredentials;
                response.SessionToken = new byte[32];
                response.WebToken = string.Empty;
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
            response.SessionToken = session.SessionToken;
            response.WebToken = session.WebToken;
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
