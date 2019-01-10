using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network
{
    internal interface IPacketHandler
    {
        Task Handle(P00ConnectionHandshake packet);
        //Task Handle(P01ConnectionResponse packet);
        Task Handle(P02CreateAccount packet);
        //Task Handle(P03CreateAccountResponse packet);
        Task Handle(P04DeleteAccount packet);
        //Task Handle(P05DeleteAccountResponse packet);
        Task Handle(P06CreateSession packet);
        //Task Handle(P07CreateSessionResponse packet);
        Task Handle(P08RestoreSession packet);
        //Task Handle(P09RestoreSessionResponse packet);
        Task Handle(P0ACreateChannel packet);
        Task Handle(P0BChannelMessage packet);
        //Task Handle(P0CChannelMessageResponse packet);
        Task Handle(P0DMessageBlock packet);
        Task Handle(P0ERequestMessages packet);
        //Task Handle(P0FSyncFinished packet);
        Task Handle(P10RealTimeMessage packet);
        Task Handle(P11SubscribeChannel packet);
        Task Handle(P12UnsubscribeChannel packet);

        Task Handle(P13QueueMailAddressChange packet);
        //Task Handle(P14MailAddress packet);
        Task Handle(P15PasswordUpdate packet);
        Task Handle(P18PublicKeys packet);
        //Task Handle(P19DerivationKey packet);
        //Task Handle(P1BDirectChannelUpdate packet);
        Task Handle(P1EGroupChannelUpdate packet);
        Task Handle(P22MessageReceived packet);
        Task Handle(P23MessageRead packet);
        Task Handle(P25Nickname packet);
        Task Handle(P26PersonalMessage packet);
        Task Handle(P27ProfileImage packet);
        Task Handle(P28BlockList packet);
        //Task Handle(P29DeviceList packet);

        Task Handle(P2DSearchAccount packet);
        //Task Handle(P2ESearchAccountResponse packet);
        Task Handle(P30FileUpload packet);
        //Task Handle(P31FileUploadResponse packet);
    }
}
