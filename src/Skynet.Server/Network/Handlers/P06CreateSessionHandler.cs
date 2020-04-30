using Microsoft.EntityFrameworkCore;
using Skynet.Protocol.Model;
using Skynet.Protocol.Packets;
using Skynet.Server.Database.Entities;
using Skynet.Server.Services;
using Skynet.Server.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
            using var csp = SHA256.Create();
            byte[] passwordHash = csp.ComputeHash(packet.KeyHash);

            // As of RFC 5321 the local-part of an email address should not be case-sensitive.
            // EF Core converts the C# == operator to = in SQL which compares the contents of byte arrays
            var confirmation = await
                (from c in Database.MailConfirmations.AsQueryable().Where(c => c.MailAddress == packet.AccountName.ToLowerInvariant())
                 join a in Database.Accounts.AsQueryable().Where(a => a.PasswordHash == passwordHash)
                     on c.AccountId equals a.AccountId
                 select c)
                 .SingleOrDefaultAsync().ConfigureAwait(false);

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

            byte[] sessionToken = SkynetRandom.Bytes(32);
            byte[] sessionTokenHash = csp.ComputeHash(sessionToken);
            string webToken = SkynetRandom.String(30);
            byte[] webTokenHash = csp.ComputeHash(Encoding.UTF8.GetBytes(webToken));

            Session session = await Database.AddSession(new Session
            {
                AccountId = confirmation.AccountId,
                SessionTokenHash = sessionTokenHash,
                WebTokenHash = webTokenHash,
                ApplicationIdentifier = Client.ApplicationIdentifier,
                LastConnected = DateTime.Now,
                LastVersionCode = Client.VersionCode,
                FcmToken = packet.FcmRegistrationToken
            }).ConfigureAwait(false);

            Message deviceList = await injector.CreateDeviceList(confirmation.AccountId).ConfigureAwait(false);

            Client.Authenticate(confirmation.AccountId, session.SessionId);

            response.StatusCode = CreateSessionStatus.Success;
            response.AccountId = session.AccountId;
            response.SessionId = session.SessionId;
            response.SessionToken = sessionToken;
            response.WebToken = webToken;
            await Client.Send(response).ConfigureAwait(false);

            await Delivery.StartSyncChannels(Client, new List<long>(), lastMessageId: default).ConfigureAwait(false);
            await Delivery.StartSendMessage(deviceList, null).ConfigureAwait(false);

            IClient old = connections.Add(Client);
            if (old != null)
            {
                _ = old.DisposeAsync(unregister: false);
            }
        }
    }
}
