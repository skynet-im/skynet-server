using Microsoft.EntityFrameworkCore;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Model;
using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public static P0BChannelMessage ToPacket(this Message message)
        {
            var packet = Packet.New<P0BChannelMessage>();
            packet.ChannelId = message.ChannelId;
            packet.SenderId = message.SenderId ?? 0;
            packet.MessageId = message.MessageId;
            packet.SkipCount = 0; // TODO: Implement flags and skip count
            packet.DispatchTime = DateTime.SpecifyKind(message.DispatchTime, DateTimeKind.Local);
            packet.MessageFlags = message.MessageFlags;
            packet.FileId = 0; // Files are not implemented yet
            packet.Dependencies.AddRange(message.Dependencies.ToProtocol());
            packet.ContentPacketId = message.ContentPacketId;
            packet.ContentPacketVersion = message.ContentPacketVersion;
            packet.ContentPacket = message.ContentPacket;
            return packet;
        }

        public static Task<Message> GetLatestPublicKey(this Account account, DatabaseContext ctx)
        {
            return ctx.Channels.Where(c => c.ChannelType == ChannelType.AccountData && c.OwnerId == account.AccountId)
                .Join(ctx.Messages, c => c.ChannelId, m => m.ChannelId, (c, m) => m)
                .Where(m => m.ContentPacketId == 0x18)
                .OrderByDescending(m => m.MessageId).FirstOrDefaultAsync();
        }
    }
}
