using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Network
{
    internal interface IPacketHandler
    {
        void Handle(P00ConnectionHandshake packet);
        //void Handle(P01ConnectionResponse packet);
        void Handle(P02CreateAccount packet);
        //void Handle(P03CreateAccountResponse packet);
        void Handle(P04DeleteAccount packet);
        //void Handle(P05DeleteAccountResponse packet);
        void Handle(P06CreateSession packet);
        //void Handle(P07CreateSessionResponse packet);
        void Handle(P08RestoreSession packet);
        //void Handle(P09RestoreSessionResponse packet);
        void Handle(P0ACreateChannel packet);
        void Handle(P0BChannelMessage packet);
        //void Handle(P0CChannelMessageResponse packet);
        void Handle(P0DMessageBlock packet);
        void Handle(P0ERequestMessages packet);
        //void Handle(P0FSyncFinished packet);
        void Handle(P10RealTimeMessage packet);
        void Handle(P11SubscribeChannel packet);
        void Handle(P12UnsubscribeChannel packet);

        void Handle(P13QueueMailAddressChange packet);
        //void Handle(P14MailAddress packet);
        void Handle(P15PasswordUpdate packet);
        void Handle(P18PublicKeys packet);
        //void Handle(P19DerivationKey packet);
        //void Handle(P1BDirectChannelUpdate packet);
        void Handle(P1EGroupChannelUpdate packet);
        void Handle(P22MessageReceived packet);
        void Handle(P23MessageRead packet);
        void Handle(P25Nickname packet);
        void Handle(P26PersonalMessage packet);
        void Handle(P27ProfileImage packet);
        void Handle(P28BlockList packet);
        //void Handle(P29DeviceList packet);

        void Handle(P2DSearchAccount packet);
        //void Handle(P2ESearchAccountResponse packet);
    }
}
