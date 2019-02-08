﻿using Microsoft.Extensions.Configuration;
using SkynetServer.Configuration;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Model;
using SkynetServer.Network.Mail;
using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VSL;
using VSL.BinaryTools;

namespace SkynetServer.Network
{
    internal partial class Client
    {
        private string applicationIdentifier;
        private int versionCode;

        public Task Handle(P00ConnectionHandshake packet)
        {
            ProtocolConfig config = Program.Configuration.Get<SkynetConfig>().ProtocolConfig;
            var response = Packet.New<P01ConnectionResponse>();
            response.LatestVersion = config.VersionName;
            response.LatestVersionCode = config.VersionCode;
            if (packet.ProtocolVersion != config.ProtocolVersion || packet.VersionCode < config.ForceUpdateThreshold)
                response.ConnectionState = ConnectionState.MustUpgrade;
            else if (packet.VersionCode < config.RecommendUpdateThreshold)
                response.ConnectionState = ConnectionState.CanUpgrade;
            else
                response.ConnectionState = ConnectionState.Valid;

            applicationIdentifier = packet.ApplicationIdentifier;
            versionCode = packet.VersionCode;

            return SendPacket(response);
        }

        public async Task Handle(P02CreateAccount packet)
        {
            using (var ctx = new DatabaseContext())
            {
                var response = Packet.New<P03CreateAccountResponse>();
                if (!ConfirmationMailer.IsValidEmail(packet.AccountName))
                    response.ErrorCode = CreateAccountError.InvalidAccountName;
                else
                {
                    (var account, var confirmation, bool success) = await DatabaseHelper.AddAccount(packet.AccountName, packet.KeyHash);
                    if (!success)
                        response.ErrorCode = CreateAccountError.AccountNameTaken;
                    else
                    {
                        Task mail = new ConfirmationMailer().SendMailAsync(confirmation.MailAddress, confirmation.Token);
                        Channel channel = await DatabaseHelper.AddChannel(new Channel()
                        {
                            ChannelType = ChannelType.Loopback,
                            OwnerId = account.AccountId
                        });
                        ctx.ChannelMembers.Add(new ChannelMember { Channel = channel, Account = account });
                        await ctx.SaveChangesAsync();

                        // Send password update packet
                        var passwordUpdate = Packet.New<P15PasswordUpdate>();
                        passwordUpdate.KeyHash = packet.KeyHash;
                        using (PacketBuffer buffer = PacketBuffer.CreateDynamic())
                        {
                            packet.WritePacket(buffer);
                            ctx.Messages.Add(new Message()
                            {
                                Channel = channel,
                                Sender = account,
                                DispatchTime = DateTime.Now,
                                MessageFlags = MessageFlags.Unencrypted,
                                ContentPacketId = packet.Id,
                                ContentPacketVersion = 0,
                                ContentPacket = buffer.ToArray()
                            });
                            await ctx.SaveChangesAsync();
                        }

                        await mail;
                        response.ErrorCode = CreateAccountError.Success;
                    }
                }
                await SendPacket(response);
            }
        }

        public Task Handle(P04DeleteAccount packet)
        {
            throw new NotImplementedException();
        }

        public async Task Handle(P06CreateSession packet)
        {
            using (var ctx = new DatabaseContext())
            {
                var response = Packet.New<P07CreateSessionResponse>();

                var confirmation = ctx.MailConfirmations.SingleOrDefault(c => c.MailAddress == packet.AccountName);
                if (confirmation == null)
                    response.ErrorCode = CreateSessionError.InvalidCredentials;
                else if (confirmation.ConfirmationTime == default)
                    response.ErrorCode = CreateSessionError.UnconfirmedAccount;
                else if (packet.KeyHash.SafeEquals(confirmation.Account.KeyHash))
                {
                    session = await DatabaseHelper.AddSession(new Session
                    {
                        Account = confirmation.Account,
                        ApplicationIdentifier = applicationIdentifier,
                        CreationTime = DateTime.Now,
                        LastConnected = DateTime.Now,
                        LastVersionCode = versionCode,
                        FcmToken = packet.FcmRegistrationToken
                    });

                    account = confirmation.Account;

                    response.AccountId = account.AccountId;
                    response.SessionId = session.SessionId;
                    response.ErrorCode = CreateSessionError.Success;
                }
                else
                    response.ErrorCode = CreateSessionError.InvalidCredentials;
                await SendPacket(response);
                await SendMessages(new List<(long channelId, long messageId)>());
            }
        }

        public async Task Handle(P08RestoreSession packet)
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
                await SendPacket(response);
                await SendMessages(packet.Channels);
            }
        }

        public async Task SendMessages(List<(long channelId, long messageId)> currentState)
        {
            using (DatabaseContext ctx = new DatabaseContext())
            {
                Channel[] channels = account.ChannelMemberships.Select(m => m.Channel).ToArray();

                foreach (Channel channel in channels)
                {
                    if (!currentState.Any(s => s.channelId == channel.ChannelId))
                    {
                        // Notify client about new channels
                        var packet = Packet.New<P0ACreateChannel>();
                        packet.ChannelId = channel.ChannelId;
                        packet.ChannelType = channel.ChannelType;
                        packet.OwnerId = channel.OwnerId ?? 0;
                        if (packet.ChannelType == ChannelType.Direct)
                            packet.CounterpartId = channel.ChannelMembers.Single(m => m.AccountId != channel.OwnerId).AccountId;
                        await SendPacket(packet);
                        currentState.Add((channel.ChannelId, 0));
                    }
                }

                // Send messages from loopback channel
                Channel loopback = channels.Single(c => c.ChannelType == ChannelType.Loopback);
                long lastLoopbackMessage = currentState.Single(s => s.channelId == loopback.ChannelId).messageId;
                foreach (Message message in loopback.Messages.Where(m => m.MessageId > lastLoopbackMessage))
                {
                    var packet = Packet.New<P0BChannelMessage>();
                    packet.ChannelId = loopback.ChannelId;
                    packet.SenderId = message.SenderId ?? 0;
                    packet.MessageId = message.MessageId;
                    packet.SkipCount = 0; // TODO: Implement flags and skip count
                    packet.DispatchTime = message.DispatchTime;
                    packet.MessageFlags = message.MessageFlags;
                    packet.FileId = 0; // Files are not implemented yet
                                       // TODO: Implement dependencies
                    packet.ContentPacketId = message.ContentPacketId;
                    packet.ContentPacketVersion = message.ContentPacketVersion;
                    packet.ContentPacket = message.ContentPacket;
                    await SendPacket(packet);
                }

                // Send messages from direct channels
                foreach (Channel channel in account.ChannelMemberships.Select(m => m.Channel).Where(c => c.ChannelType == ChannelType.Direct))
                {
                    long lastMessage = currentState.Single(s => s.channelId == channel.ChannelId).messageId;
                    foreach (Message message in channel.Messages.Where(m => m.MessageId > lastMessage))
                    {
                        var packet = Packet.New<P0BChannelMessage>();
                        packet.ChannelId = loopback.ChannelId;
                        packet.SenderId = message.SenderId ?? 0;
                        packet.MessageId = message.MessageId;
                        packet.SkipCount = 0; // TODO: Implement flags and skip count
                        packet.DispatchTime = message.DispatchTime;
                        packet.MessageFlags = message.MessageFlags;
                        packet.FileId = 0; // Files are not implemented yet
                                           // TODO: Implement dependencies
                        packet.ContentPacketId = message.ContentPacketId;
                        packet.ContentPacketVersion = message.ContentPacketVersion;
                        packet.ContentPacket = message.ContentPacket;
                        await SendPacket(packet);
                    }
                }

                await SendPacket(Packet.New<P0FSyncFinished>());
            }
        }

        public async Task Handle(P0ACreateChannel packet)
        {
            using (var ctx = new DatabaseContext())
            {
                Channel channel = null;
                var response = Packet.New<P2FCreateChannelResponse>();
                switch (packet.ChannelType)
                {
                    case ChannelType.Direct:
                        var counterpart = ctx.Accounts.SingleOrDefault(acc => acc.AccountId == packet.CounterpartId);
                        if (counterpart == null)
                        {
                            response.ErrorCode = CreateChannelError.InvalidCounterpart;
                        }
                        else if (counterpart.BlockedAccounts.Any(acc => acc.AccountId == account.AccountId)
                                 || account.BlockedAccounts.Any(acc => acc.AccountId == packet.CounterpartId))
                            response.ErrorCode = CreateChannelError.Blocked;
                        else
                        {
                            // TODO: Check whether a direct channel exists before and after inserting
                            channel = await DatabaseHelper.AddChannel(new Channel
                            {
                                Owner = account,
                                ChannelType = ChannelType.Direct
                            });

                            ctx.ChannelMembers.Add(new ChannelMember { Channel = channel, Account = account });
                            ctx.ChannelMembers.Add(new ChannelMember { Channel = channel, AccountId = packet.CounterpartId });
                            await ctx.SaveChangesAsync();

                            response.ErrorCode = CreateChannelError.Success;
                        }
                        break;
                    case ChannelType.Group:
                        channel = await DatabaseHelper.AddChannel(new Channel
                        {
                            Owner = account,
                            ChannelType = packet.ChannelType
                        });

                        response.ErrorCode = CreateChannelError.Success;
                        break;
                    case ChannelType.ProfileData:
                    case ChannelType.Loopback:
                        if (account.OwnedChannels.Any(c => c.ChannelType == packet.ChannelType))
                            response.ErrorCode = CreateChannelError.AlreadyExists;
                        else
                        {
                            channel = await DatabaseHelper.AddChannel(new Channel
                            {
                                Owner = account,
                                ChannelType = packet.ChannelType
                            });

                            ctx.ChannelMembers.Add(new ChannelMember { Channel = channel, Account = account });
                            await ctx.SaveChangesAsync();

                            response.ErrorCode = CreateChannelError.Success;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (channel != null)
                {
                    response.ChannelId = channel.ChannelId;
                    response.TempChannelId = packet.ChannelId;
                }
                await SendPacket(response);
            }
        }

        public Task Handle(P0BChannelMessage packet)
        {
            if (packet.ContentPacketId < 0x13 || packet.ContentPacketId > 0x2A)
                throw new ProtocolException("Invalid content packet ID");

            if (packet.MessageFlags.HasFlag(MessageFlags.Unencrypted))
            {
                if (!(Packet.Packets[packet.ContentPacketId] is P0BChannelMessage message) || !message.Policy.HasFlag(PacketPolicy.Receive))
                    throw new ProtocolException("Content packet is no receivable channel message");

                message.ChannelId = packet.ChannelId;
                message.SenderId = packet.SenderId;
                message.MessageId = packet.MessageId;
                message.SkipCount = packet.SkipCount;
                message.DispatchTime = packet.DispatchTime;
                message.MessageFlags = packet.MessageFlags;
                message.FileId = packet.FileId;
                message.Dependencies = packet.Dependencies;

                using (PacketBuffer buffer = PacketBuffer.CreateStatic(packet.ContentPacket))
                    message.ReadPacket(buffer);

                return message.Handle(this);
                // TODO: Not all messages can be saved, some return MessageSendError other than Success
            }
            else
            {
                // TODO: Save packet in DB and send to channel
                throw new NotImplementedException();
            }
        }

        public Task Handle(P0DMessageBlock packet)
        {
            // TODO: Implement transaction system for password changes
            throw new NotImplementedException();
        }

        public Task Handle(P0ERequestMessages packet)
        {
            // TODO: Send messages using a similar pattern like SendMessages()
            throw new NotImplementedException();
        }

        public Task Handle(P10RealTimeMessage packet)
        {
            throw new NotImplementedException();
        }

        public Task Handle(P11SubscribeChannel packet)
        {
            throw new NotImplementedException();
        }

        public Task Handle(P12UnsubscribeChannel packet)
        {
            throw new NotImplementedException();
        }

        public Task<MessageSendError> Handle(P13QueueMailAddressChange packet)
        {
            throw new NotImplementedException();
        }

        public Task<MessageSendError> Handle(P15PasswordUpdate packet)
        {
            // TODO: Inject dependency from previous PasswordUpdate to latest LoopbackKeyNotify packet
            throw new NotImplementedException();
        }

        public Task<MessageSendError> Handle(P18PublicKeys packet)
        {
            // TODO: Save message in loopback channel and forward to all direct channels
            throw new NotImplementedException();
        }

        public Task<MessageSendError> Handle(P1EGroupChannelUpdate packet)
        {
            // TODO: Check for concurrency issues before insert
            throw new NotImplementedException();
        }

        public Task<MessageSendError> Handle(P22MessageReceived packet)
        {
            throw new NotImplementedException();
        }

        public Task<MessageSendError> Handle(P23MessageRead packet)
        {
            throw new NotImplementedException();
        }

        public Task<MessageSendError> Handle(P25Nickname packet)
        {
            throw new NotImplementedException();
        }

        public Task<MessageSendError> Handle(P26PersonalMessage packet)
        {
            throw new NotImplementedException();
        }

        public Task<MessageSendError> Handle(P27ProfileImage packet)
        {
            throw new NotImplementedException();
        }

        public Task<MessageSendError> Handle(P28BlockList packet)
        {
            // TODO: What happens with existing channels?
            throw new NotImplementedException();
        }

        public Task Handle(P2DSearchAccount packet)
        {
            using (var ctx = new DatabaseContext())
            {
                var results = ctx.MailConfirmations
                    .Where(c => c.MailAddress.Contains(packet.Query)
                        && c.ConfirmationTime != default) // Exclude unconfirmed accounts
                    .Take(100); // Limit to 100 entries
                var response = Packet.New<P2ESearchAccountResponse>();
                foreach (var result in results)
                    response.Results.Add(new SearchResult
                    {
                        AccountId = result.AccountId,
                        AccountName = result.MailAddress
                        // Forward public packets to fully implement the Skynet protocol v5
                    });
                return SendPacket(response);
            }
        }

        public Task Handle(P30FileUpload packet)
        {
            throw new NotImplementedException();
        }
    }
}
