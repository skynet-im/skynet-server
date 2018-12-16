using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Packets
{
    internal interface IPacketHandler
    {
        void Handle(P00ConnectionHandshake packet);
        void Handle(P01ConnectionResponse packet);
        void Handle(P02CreateAccount packet);
        void Handle(P03CreateAccountResponse packet);
        void Handle(P04DeleteAccount packet);
        void Handle(P05DeleteAccountResponse packet);
        void Handle(P06CreateSession packet);
        void Handle(P07CreateSessionResponse packet);
        void Handle(P08RestoreSession packet);
        void Handle(P09RestoreSessionResponse packet);
    }
}
