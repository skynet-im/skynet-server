using Microsoft.EntityFrameworkCore;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Model;
using SkynetServer.Network;
using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Services
{
    internal class DeliveryService
    {
        private readonly FirebaseService firebase;
        private ImmutableList<Client> clients;

        public DeliveryService(FirebaseService firebase)
        {
            this.firebase = firebase;
            clients = ImmutableList.Create<Client>();
        }

        public void Register(Client client)
        {
            ImmutableInterlocked.Update(ref clients, list => list.Add(client));
        }

        public void Unregister(Client client)
        {
            ImmutableInterlocked.Update(ref clients, list => list.Remove(client));
        }

        public async Task<Message> SendMessage(P0BChannelMessage packet, Channel channel, long? senderId)
        {
            packet.ChannelId = channel.ChannelId;
            packet.MessageFlags |= MessageFlags.Unencrypted;
            if (packet.ContentPacket == null)
            {
                using (PacketBuffer buffer = PacketBuffer.CreateDynamic())
                {
                    packet.WriteMessage(buffer);
                    packet.ContentPacket = buffer.ToArray();
                }
            }

            Message message = new Message()
            {
                ChannelId = channel.ChannelId,
                SenderId = senderId,
                // TODO: Implement skip count
                MessageFlags = packet.MessageFlags,
                // TODO: Implement FileId
                ContentPacketId = packet.ContentPacketId,
                ContentPacketVersion = packet.ContentPacketVersion,
                ContentPacket = packet.ContentPacket
            };

            message = await DatabaseHelper.AddMessage(message, packet.Dependencies.ToDatabase());

            packet.SenderId = message.SenderId ?? 0;
            packet.MessageId = message.MessageId;
            packet.DispatchTime = DateTime.SpecifyKind(message.DispatchTime, DateTimeKind.Local);

            using (DatabaseContext ctx = new DatabaseContext())
            {
                long[] members = await ctx.ChannelMembers.Where(m => m.ChannelId == channel.ChannelId).Select(m => m.AccountId).ToArrayAsync();
                bool isLoopback = packet.MessageFlags.HasFlag(MessageFlags.Loopback);
                bool isNoSenderSync = packet.MessageFlags.HasFlag(MessageFlags.NoSenderSync);
                await Task.WhenAll(clients
                    .Where(c => c.Account != null && members.Contains(c.Account.AccountId))
                    .Where(c => !isLoopback || c.Account.AccountId == senderId)
                    .Where(c => !isNoSenderSync || c.Account.AccountId != senderId)
                    .Select(c => c.SendPacket(packet)));
            }

            return message;
        }

        public Task SendPacket(Packet packet, long accountId, Client exclude)
        {
            return Task.WhenAll(clients
                .Where(c => c.Account != null && c.Account.AccountId == accountId && !ReferenceEquals(c, exclude))
                .Select(c => c.SendPacket(packet)));
        }

        public Task SendPacket(Packet packet, IEnumerable<long> accounts, Client exclude)
        {
            return Task.WhenAll(clients
                .Where(c => c.Account != null && accounts.Contains(c.Account.AccountId) && !ReferenceEquals(c, exclude))
                .Select(c => c.SendPacket(packet)));
        }

        public Task SendPacketOrNotify(Packet packet, IEnumerable<Session> sessions, Client exclude, long excludeFcm)
        {
            return Task.WhenAll(sessions.Select(async session =>
            {
                bool found = false;
                foreach (Client client in clients)
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
