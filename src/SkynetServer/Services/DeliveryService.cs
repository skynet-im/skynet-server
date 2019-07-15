using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Services
{
    internal class DeliveryService
    {
        private readonly FirebaseService firebase;

        public DeliveryService(FirebaseService firebase)
        {
            this.firebase = firebase;
        }

        public Task SendPacketOrNotify(Packet packet, IEnumerable<Session> sessions, Client exclude, long excludeFcm)
        {
            return Task.WhenAll(sessions.Select(async session =>
            {
                bool found = false;
                foreach (Client client in Program.Clients)
                {
                    if (client.Session != null
                        && client.Session.AccountId == session.AccountId
                        && client.Session.SessionId == session.SessionId)
                    {
                        found = true;
                        if (!ReferenceEquals(client, exclude))
                            await client.SendPacket(packet);
                        break;
                    }
                }
                if (!found)
                {
                    if (session.AccountId != excludeFcm
                        && !string.IsNullOrWhiteSpace(session.FcmToken)
                        && session.LastFcmMessage < session.LastConnected)
                    {
                        try
                        {
                            await firebase.SendAsync(session.FcmToken);

                            // Use a separate context to save changes asynchronously for multiple sessions
                            using (DatabaseContext ctx = new DatabaseContext())
                            {
                                session.LastFcmMessage = DateTime.Now;
                                ctx.Entry(session).Property(s => s.LastFcmMessage).IsModified = true;
                                await ctx.SaveChangesAsync();
                                Console.WriteLine($"Successfully sent FCM message to {session.FcmToken.Remove(16)} last connected {session.LastConnected}");
                            }
                        }
                        catch (FirebaseAdmin.FirebaseException)
                        {
                            Console.WriteLine($"Failed to send FCM message to {session.FcmToken.Remove(16)}...");
                        }
                    }
                }
            }));
        }

    }
}
