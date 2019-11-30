using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network
{
    internal interface IPacketHandler
    {
        Task HandleMessage(ChannelMessage packet);

        Task<MessageSendStatus> Handle(P13QueueMailAddressChange packet);
        //Task<MessageSendError> Handle(P14MailAddress packet);
        Task<MessageSendStatus> Handle(P15PasswordUpdate packet);
        Task<MessageSendStatus> Handle(P18PublicKeys packet);
        Task PostHandling(P18PublicKeys packet, Database.Entities.Message message);
        //Task<MessageSendError> Handle(P1BDirectChannelUpdate packet);
        Task<MessageSendStatus> Handle(P1EGroupChannelUpdate packet);
        //Task<MessageSendError> Handle(P22MessageReceived packet);
        //Task<MessageSendError> Handle(P23MessageRead packet);
        //Task<MessageSendError> Handle(P25Nickname packet);
        //Task<MessageSendError> Handle(P26PersonalMessage packet);
        //Task<MessageSendError> Handle(P27ProfileImage packet);
        Task<MessageSendStatus> Handle(P28BlockList packet);
        //Task<MessageSendError> Handle(P29DeviceList packet);
        //Task<MessageSendError> Handle(P2BOnlineState packet);
    }
}
