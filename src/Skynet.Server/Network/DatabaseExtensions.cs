using Microsoft.EntityFrameworkCore;
using Skynet.Model;
using Skynet.Protocol;
using Skynet.Protocol.Model;
using Skynet.Server.Database;
using Skynet.Server.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skynet.Server.Network
{
    internal static class DatabaseExtensions
    {
        public static List<MessageDependency> ToDatabase(this List<Dependency> dependencies)
        {
            var result = new List<MessageDependency>(dependencies.Count);
            for (int i = 0; i < dependencies.Count; i++)
            {
                result.Add(new MessageDependency
                {
                    AccountId = dependencies[i].AccountId == 0 ? null : new long?(dependencies[i].AccountId),
                    MessageId = dependencies[i].MessageId,
                });
            }
            return result;
        }

        public static ChannelMessage ToPacket(this Message message, long accountId)
        {
            var packet = new ChannelMessage
            {
                Id = message.PacketId,
                PacketVersion = message.PacketVersion,
                ChannelId = message.ChannelId,
                SenderId = message.SenderId ?? 0,
                MessageId = message.MessageId,
                SkipCount = 0, // TODO: Implement flags and skip count
                DispatchTime = DateTime.SpecifyKind(message.DispatchTime, DateTimeKind.Local),
                MessageFlags = message.MessageFlags,
                FileId = 0, // Files are not implemented yet
                PacketContent = message.PacketContent
            };

            packet.Dependencies.AddRange(message.Dependencies
                .Where(d => d.AccountId == null || d.AccountId == accountId)
                .Select(d => new Dependency(d.AccountId ?? 0, d.MessageId)));

            return packet;
        }


        public static Task<long> GetLatestPublicKey(this DatabaseContext database, long accountId)
        {
            return database.Channels.AsQueryable()
                .Where(c => c.ChannelType == ChannelType.AccountData && c.OwnerId == accountId)
                .Join(database.Messages.AsQueryable().Where(m => m.PacketId == 0x18), c => c.ChannelId, m => m.ChannelId, (c, m) => m.MessageId)
                .OrderByDescending(id => id).FirstOrDefaultAsync();
        }
    }
}
