using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using SkynetServer.Configuration;
using SkynetServer.Entities;
using SkynetServer.Network.Mail;
using SkynetServer.Network.Model;
using VSL.BinaryTools;

namespace SkynetServer.Network
{
    internal partial class Client
    {
        private string applicationIdentifier;
        private int versionCode;

        public void Handle(P00ConnectionHandshake packet)
        {
            ProtocolConfig config = Program.Configuration.Get<SkynetConfig>().ProtocolConfig;
            var response = Packet.New<P01ConnectionResponse>();
            response.LatestVersion = config.VersionName;
            response.LatestVersionCode = config.VersionCode;
            if (packet.ProtocolVersion != config.ProtocolVersion || packet.VersionCode <= config.ForceUpdateThreshold)
                response.ConnectionState = ConnectionState.MustUpgrade;
            else if (packet.VersionCode <= config.RecommendUpdateThreshold)
                response.ConnectionState = ConnectionState.CanUpgrade;
            else
                response.ConnectionState = ConnectionState.Valid;

            applicationIdentifier = packet.ApplicationIdentifier;
            versionCode = packet.VersionCode;

            SendPacket(response);
        }

        public void Handle(P02CreateAccount packet)
        {
            using (var ctx = new DatabaseContext())
            {
                var response = Packet.New<P03CreateAccountResponse>();
                // TODO: Concurrency
                if (!ConfirmationMailer.IsValidEmail(packet.AccountName))
                    response.ErrorCode = CreateAccountError.InvalidAccountName;
                else if (ctx.Accounts.Any(acc => acc.AccountName == packet.AccountName))
                    response.ErrorCode = CreateAccountError.AccountNameTaken;
                else
                {
                    ctx.AddAccount(new Account
                    {
                        AccountName = packet.AccountName,
                        KeyHash = packet.KeyHash
                    });
                    response.ErrorCode = CreateAccountError.Success;
                }
                SendPacket(response);
                // TODO: Create channels
            }
        }

        public void Handle(P04DeleteAccount packet)
        {
            throw new NotImplementedException();
        }

        public void Handle(P06CreateSession packet)
        {
            using (var ctx = new DatabaseContext())
            {
                var accountCandidate = ctx.Accounts.Single(acc => acc.AccountName == packet.AccountName);
                var response = Packet.New<P07CreateSessionResponse>();
                if (packet.KeyHash.SafeEquals(accountCandidate.KeyHash))
                {
                    session = ctx.AddSession(new Session
                    {
                        Account = accountCandidate,
                        ApplicationIdentifier = applicationIdentifier,
                        CreationTime = DateTime.Now,
                        LastConnected = DateTime.Now,
                        LastVersionCode = versionCode,
                        FcmToken = packet.FcmRegistrationToken
                    });
                    account = accountCandidate;

                    response.AccountId = account.AccountId;
                    response.SessionId = session.SessionId;
                    response.ErrorCode = CreateSessionError.Success;
                }
                else
                    response.ErrorCode = CreateSessionError.InvalidCredentials;
                SendPacket(response);
                // TODO: Send messages
            }
        }

        public void Handle(P08RestoreSession packet)
        {
            using (var ctx = new DatabaseContext())
            {
                var accountCandidate = ctx.Accounts.SingleOrDefault(acc => acc.AccountId == packet.AccountId);
                var response = Packet.New<P09RestoreSessionResponse>();
                if (accountCandidate != null && packet.KeyHash.SafeEquals(accountCandidate.KeyHash))
                {
                    session = ctx.Sessions.SingleOrDefault(s =>
                        s.AccountId == packet.AccountId && s.SessionId == packet.SessionId);

                    if (session == null)
                        response.ErrorCode = RestoreSessionError.InvalidSession;
                    else
                    {
                        account = accountCandidate;
                        response.ErrorCode = RestoreSessionError.Success;
                    }
                }
                else
                    response.ErrorCode = RestoreSessionError.InvalidCredentials;
                SendPacket(response);
                // TODO: Send messages
            }
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
