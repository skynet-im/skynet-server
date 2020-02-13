using Microsoft.EntityFrameworkCore;
using SkynetServer.Database.Entities;
using SkynetServer.Model;
using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkynetServer.Network
{
    internal class MessageHandler<T> : PacketHandler<T> where T : ChannelMessage
    {
        public sealed override async ValueTask Handle(T packet)
        {
            if (!packet.MessageFlags.AreValid(packet.RequiredFlags, packet.AllowedFlags))
                throw new ProtocolException($"Invalid MessageFlags{packet.MessageFlags} for content packet ID {packet.Id}");

            if (packet.MessageFlags.HasFlag(MessageFlags.Unencrypted)
                && await Validate(packet).ConfigureAwait(false) != MessageSendStatus.Success)
                return; // Not all messages can be saved, some return MessageSendError other than Success

            Channel channel = await Database.Channels.SingleOrDefaultAsync(c => c.ChannelId == packet.ChannelId).ConfigureAwait(false);
            if (channel == null)
                throw new ProtocolException("Attempted to send a message to a non existent channel");

            switch (channel.ChannelType)
            {
                case ChannelType.Loopback:
                case ChannelType.AccountData:
                case ChannelType.ProfileData:
                    if (channel.OwnerId != Client.AccountId)
                        throw new ProtocolException("Attempted to send a message to a foreign channel");
                    break;
                case ChannelType.Direct:
                    if (!await Database.ChannelMembers
                        .AnyAsync(m => m.ChannelId == packet.ChannelId && m.AccountId == Client.AccountId).ConfigureAwait(false))
                        throw new ProtocolException("Attempted to send a message to a foreign channel");
                    break;
                case ChannelType.Group:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentException($"Invalid value {channel.ChannelType} for enum {nameof(ChannelType)}");
            }

            Message entity = new Message
            {
                ChannelId = packet.ChannelId,
                SenderId = Client.AccountId,
                MessageFlags = packet.MessageFlags,
                // TODO: Implement FileId
                PacketId = packet.Id,
                PacketVersion = packet.PacketVersion,
                PacketContent = packet.PacketContent.IsEmpty ? null : packet.PacketContent.ToArray(),
                Dependencies = packet.Dependencies.ToDatabase()
            };
            Database.Messages.Add(entity);
            await Database.SaveChangesAsync().ConfigureAwait(false);

            var response = Packets.New<P0CChannelMessageResponse>();
            response.ChannelId = packet.ChannelId;
            response.TempMessageId = packet.MessageId;
            response.StatusCode = MessageSendStatus.Success;
            response.MessageId = entity.MessageId;
            // TODO: Implement skip count
            response.DispatchTime = DateTime.SpecifyKind(entity.DispatchTime, DateTimeKind.Local);
            await Client.Send(response).ConfigureAwait(false);

            packet.SenderId = Client.AccountId;
            packet.MessageId = entity.MessageId;
            packet.DispatchTime = DateTime.SpecifyKind(entity.DispatchTime, DateTimeKind.Local);

            if (packet.Id == 0x20)
                await Delivery.SendPriorityMessage(entity, exclude: Client, excludeFcm: Client.Account);
            else
                _ = await Delivery.SendMessage(entity, exclude: Client).ConfigureAwait(false);

            await PostHandling(packet, entity).ConfigureAwait(false);
        }

        protected virtual ValueTask<MessageSendStatus> Validate(T packet)
        {
            return new ValueTask<MessageSendStatus>(MessageSendStatus.Success);
        }

        protected virtual ValueTask PostHandling(T packet, Message message)
        {
            return default;
        }
    }
}
