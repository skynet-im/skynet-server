using Microsoft.EntityFrameworkCore;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Model;
using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer.Network
{
    internal static class DatabaseExtensions
    {
        public static IEnumerable<MessageDependency> ToDatabase(this IEnumerable<Dependency> dependencies)
        {
            foreach (Dependency dependency in dependencies)
            {
                yield return new MessageDependency()
                {
                    ChannelId = dependency.ChannelId,
                    MessageId = dependency.MessageId,
                    AccountId = dependency.AccountId == 0 ? null : new long?(dependency.AccountId)
                };
            }
        }

        public static IEnumerable<Dependency> ToProtocol(this IEnumerable<MessageDependency> dependencies)
        {
            foreach (MessageDependency dependency in dependencies)
            {
                yield return new Dependency(dependency.AccountId ?? 0, dependency.ChannelId, dependency.MessageId);
            }
        }

        public static async Task<Message> SendMessage(this Channel channel, P0BChannelMessage packet, long? senderId)
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
                DispatchTime = DateTime.Now,
                MessageFlags = packet.MessageFlags,
                // TODO: Implement FileId
                ContentPacketId = packet.ContentPacketId,
                ContentPacketVersion = packet.ContentPacketVersion,
                ContentPacket = packet.ContentPacket
            };

            await DatabaseHelper.AddMessage(message, packet.Dependencies.ToDatabase());

            using (DatabaseContext ctx = new DatabaseContext())
            {
                long[] members = ctx.ChannelMembers.Where(m => m.ChannelId == channel.ChannelId).Select(m => m.AccountId).ToArray();
                bool isLoopback = packet.MessageFlags.HasFlag(MessageFlags.Loopback);
                bool isNoSenderSync = packet.MessageFlags.HasFlag(MessageFlags.NoSenderSync);
                await Task.WhenAll(Program.Clients
                    .Where(c => c.Account != null && members.Contains(c.Account.AccountId))
                    .Where(c => !isLoopback || c.Account.AccountId == senderId)
                    .Where(c => !isNoSenderSync || c.Account.AccountId != senderId)
                    .Select(c => c.SendPacket(packet)));
            }

            return message;
        }

        public static Task SendTo(this Message message, Client target)
        {
            var packet = Packet.New<P0BChannelMessage>();
            packet.ChannelId = message.ChannelId;
            packet.SenderId = message.SenderId ?? 0;
            packet.MessageId = message.MessageId;
            packet.SkipCount = 0; // TODO: Implement flags and skip count
            packet.DispatchTime = message.DispatchTime;
            packet.MessageFlags = message.MessageFlags;
            packet.FileId = 0; // Files are not implemented yet
            packet.Dependencies.AddRange(message.Dependencies.ToProtocol());
            packet.ContentPacketId = message.ContentPacketId;
            packet.ContentPacketVersion = message.ContentPacketVersion;
            packet.ContentPacket = message.ContentPacket;
            return target.SendPacket(packet);
        }

        public static async Task<Message> GetLatestPublicKey(this Account account)
        {
            using (DatabaseContext ctx = new DatabaseContext())
            {
                long loopback = await ctx.Channels
                    .Where(c => c.OwnerId == account.AccountId && c.ChannelType == ChannelType.Loopback)
                    .Select(c => c.ChannelId).SingleAsync();

                return await ctx.Messages
                    .Where(m => m.ChannelId == loopback && m.ContentPacketId == 0x18 && m.SenderId == account.AccountId)
                    .OrderByDescending(m => m.MessageId).FirstOrDefaultAsync();
            }
        }
    }
}
