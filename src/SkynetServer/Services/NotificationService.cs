﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SkynetServer.Configuration;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkynetServer.Services
{
    internal sealed class NotificationService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly FirebaseService firebase;
        private readonly ConnectionsService connections;
        private readonly IOptions<FcmOptions> options;

        public NotificationService(IServiceProvider serviceProvider, FirebaseService firebase, ConnectionsService connections, IOptions<FcmOptions> options)
        {
            this.serviceProvider = serviceProvider;
            this.firebase = firebase;
            this.connections = connections;
            this.options = options;
        }

        public async Task SendFcmNotification(long accountId)
        {
            Session[] sessions;

            using (IServiceScope scope = serviceProvider.CreateScope())
            {
                var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

                sessions = await database.Sessions.AsQueryable()
                    .Where(s => s.AccountId == accountId && s.FcmToken != null
                        && (s.LastFcmMessage < s.LastConnected || options.Value.NotifyForEveryMessage))
                    .ToArrayAsync().ConfigureAwait(false);
            }

            async Task process(Session session)
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                var injector = scope.ServiceProvider.GetRequiredService<MessageInjectionService>();
                var delivery = scope.ServiceProvider.GetRequiredService<DeliveryService>();

                try
                {
                    await firebase.SendAsync(session.FcmToken).ConfigureAwait(false);

                    session.LastFcmMessage = DateTime.Now;
                    database.Entry(session).Property(s => s.LastFcmMessage).IsModified = true;
                    await database.SaveChangesAsync().ConfigureAwait(false);
                    Console.WriteLine($"Successfully sent FCM message to {session.FcmToken.Remove(16)} last connected {session.LastConnected}");
                }
                catch (FirebaseAdmin.FirebaseException ex)
                {
                    Console.WriteLine($"Failed to send FCM message to {session.FcmToken.Remove(16)}... {ex.Message}");
                    if (options.Value.DeleteSessionOnError)
                    {
                        // Prevent quick re-login after kick
                        session.SessionToken = Array.Empty<byte>();
                        database.Entry(session).Property(s => s.SessionToken).IsModified = true;
                        await database.SaveChangesAsync().ConfigureAwait(false);

                        // Kick client if connected to avoid conflicting information in RAM vs DB
                        if (connections.TryGet(session.SessionId, out IClient client))
                        {
                            await client.DisposeAsync().ConfigureAwait(false);
                        }
                        database.Sessions.Remove(session);
                        await database.SaveChangesAsync().ConfigureAwait(false);

                        Message deviceList = await injector.CreateDeviceList(session.AccountId).ConfigureAwait(false);
                        _ = await delivery.SendMessage(deviceList, null).ConfigureAwait(false);
                    }
                }
            }

            await Task.WhenAll(sessions.Select(s => process(s))).ConfigureAwait(false);
        }
    }
}
