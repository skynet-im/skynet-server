using Microsoft.EntityFrameworkCore;
using Skynet.Model;
using Skynet.Protocol;
using Skynet.Protocol.Model;
using Skynet.Server.Database;
using Skynet.Server.Database.Entities;
using Skynet.Server.Services;
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

        public static ChannelMessage ToPacket(this Message message, PacketService packets, long accountId)
        {
            Packet prototype = packets.Packets[message.PacketId];
            if (prototype == null) throw new ArgumentException($"Could not find packet {message.PacketId:x2}", nameof(message));

            if (!(prototype is ChannelMessage protoMessage)) 
                throw new ArgumentException($"Packet {message.PacketId:x2} is not a channel message", nameof(message));

            ChannelMessage packet = (ChannelMessage)prototype.Create();
            packet.Id = message.PacketId;
            packet.PacketVersion = message.PacketVersion;
            packet.ChannelId = message.ChannelId;
            packet.SenderId = message.SenderId ?? 0;
            packet.MessageId = message.MessageId;
            packet.SkipCount = 0; // TODO: Implement flags and skip count
            packet.DispatchTime = DateTime.SpecifyKind(message.DispatchTime, DateTimeKind.Local);
            packet.MessageFlags = message.MessageFlags;
            packet.FileId = 0; // Files are not implemented yet
            packet.PacketContent = message.PacketContent;

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
