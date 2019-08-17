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
        public static List<MessageDependency> ToDatabase(this List<Dependency> dependencies)
        {
            MessageDependency[] result = new MessageDependency[dependencies.Count];
            for (int i = 0; i < dependencies.Count; i++)
            {
                result[i] = new MessageDependency
                {
                    ChannelId = dependencies[i].ChannelId,
                    MessageId = dependencies[i].MessageId,
                    AccountId = dependencies[i].AccountId == 0 ? null : new long?(dependencies[i].AccountId)
                };
            }
            return new List<MessageDependency>(result);
        }

        public static P0BChannelMessage ToPacket(this Message message, long accountId)
        {
            var packet = Packet.New<P0BChannelMessage>();
            packet.ChannelId = message.ChannelId;
            packet.SenderId = message.SenderId ?? 0;
            packet.MessageId = message.MessageId;
            packet.SkipCount = 0; // TODO: Implement flags and skip count
            packet.DispatchTime = DateTime.SpecifyKind(message.DispatchTime, DateTimeKind.Local);
            packet.MessageFlags = message.MessageFlags;
            packet.FileId = 0; // Files are not implemented yet
            packet.Dependencies.AddRange(message.Dependencies
                .Where(d => d.AccountId == null || d.AccountId == accountId)
                .Select(d => new Dependency(d.AccountId ?? 0, d.ChannelId, d.MessageId)));
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
