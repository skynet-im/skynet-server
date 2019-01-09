using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using SkynetServer.Configuration;
using SkynetServer.Network.Model;

namespace SkynetServer.Network
{
    internal partial class Client
    {
        public void Handle(P00ConnectionHandshake packet)
        {
            var config = Program.Configuration.Get<ProtocolConfig>();
            var response = Packet.New<P01ConnectionResponse>();
            response.LatestVersion = config.VersionName;
            response.LatestVersionCode = config.VersionCode;
            if (packet.ProtocolVersion != config.ProtocolVersion || packet.VersionCode <= config.ForceUpdateThreshold)
                response.ConnectionState = ConnectionState.MustUpgrade;
            else if (packet.VersionCode <= config.RecommendUpdateThreshold)
                response.ConnectionState = ConnectionState.CanUpgrade;
            else
                response.ConnectionState = ConnectionState.Valid;
            SendPacket(response);
        }

        public void Handle(P02CreateAccount packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P04DeleteAccount packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P06CreateSession packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P08RestoreSession packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P0ACreateChannel packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P0BChannelMessage packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P0DMessageBlock packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P0ERequestMessages packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P10RealTimeMessage packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P11SubscribeChannel packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P12UnsubscribeChannel packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P13QueueMailAddressChange packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P15PasswordUpdate packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P18PublicKeys packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P1EGroupChannelUpdate packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P22MessageReceived packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P23MessageRead packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P25Nickname packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P26PersonalMessage packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P27ProfileImage packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P28BlockList packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P2DSearchAccount packet)
        {
            throw new NotImplementedException();
        }
    }
}
