using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using VSL;
using DatabaseDependency = SkynetServer.Database.Entities.MessageDependency;
using ProtocolDependency = SkynetServer.Network.Model.MessageDependency;

namespace SkynetServer.Network
{
    internal static class DatabaseExtensions
    {
        public static IEnumerable<DatabaseDependency> ToDatabase(this IEnumerable<ProtocolDependency> dependencies)
        {
            foreach (ProtocolDependency dependency in dependencies)
            {
                yield return new DatabaseDependency()
                {
                    ChannelId = dependency.ChannelId,
                    MessageId = dependency.MessageId,
                    AccountId = dependency.AccountId == 0 ? null : new long?(dependency.AccountId)
                };
            }
        }

        public static async Task<Message> SendMessage(this Channel channel, P0BChannelMessage packet, long? senderId)
        {
            packet.ChannelId = channel.ChannelId;
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

            return message;
        }
    }
}
