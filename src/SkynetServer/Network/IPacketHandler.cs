﻿using SkynetServer.Network.Model;
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

        Task Handle(P08RestoreSession packet);
        //Task Handle(P09RestoreSessionResponse packet);
        Task Handle(P0ACreateChannel packet);
        //Task Handle(P0DDeleteChannel packet);
        //Task Handle(P0CChannelMessageResponse packet);
        Task Handle(P0ERequestMessages packet);
        //Task Handle(P0FSyncFinished packet);

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

        //Task Handle(P2CChannelAction packet);
        Task Handle(P34SetClientState packet);

        Task Handle(P2DSearchAccount packet);
        //Task Handle(P2ESearchAccountResponse packet);
        Task Handle(P32DeviceListRequest packet);
        // Task Handle(P33DeviceListResponse packet);
    }
}
