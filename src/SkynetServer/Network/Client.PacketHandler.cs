using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
                        ctx.ChannelMembers.Add(new ChannelMember { ChannelId = channel.ChannelId, AccountId = account.AccountId });
                        await ctx.SaveChangesAsync();

                        // Send password update packet
                        var passwordUpdate = Packet.New<P15PasswordUpdate>();
                        passwordUpdate.KeyHash = packet.KeyHash;
                        using (PacketBuffer buffer = PacketBuffer.CreateDynamic())
                        {
                            passwordUpdate.WriteMessage(buffer);
                            await DatabaseHelper.AddMessage(new Message()
                            {
                                ChannelId = channel.ChannelId,
                                SenderId = account.AccountId,
                                DispatchTime = DateTime.Now,
                                MessageFlags = MessageFlags.Unencrypted,
                                ContentPacketId = packet.Id,
                                ContentPacketVersion = 0,
                                ContentPacket = buffer.ToArray()
                            });
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

                var confirmation = ctx.MailConfirmations.Include(c => c.Account).SingleOrDefault(c => c.MailAddress == packet.AccountName);
                if (confirmation == null)
                    response.ErrorCode = CreateSessionError.InvalidCredentials;
                else if (confirmation.ConfirmationTime == default)
                    response.ErrorCode = CreateSessionError.UnconfirmedAccount;
                else if (packet.KeyHash.SafeEquals(confirmation.Account.KeyHash))
                {
                    Session = await DatabaseHelper.AddSession(new Session
                    {
                        AccountId = confirmation.AccountId,
                        ApplicationIdentifier = applicationIdentifier,
                        CreationTime = DateTime.Now,
                        LastConnected = DateTime.Now,
                        LastVersionCode = versionCode,
                        FcmToken = packet.FcmRegistrationToken
                    });

                    Account = confirmation.Account;

                    response.AccountId = Account.AccountId;
                    response.SessionId = Session.SessionId;
                    response.ErrorCode = CreateSessionError.Success;
                    await SendPacket(response);
                    await SendMessages(new List<(long channelId, long messageId)>());
                    return;
                }
                else
                    response.ErrorCode = CreateSessionError.InvalidCredentials;
                await SendPacket(response);
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
                    Session = ctx.Sessions.SingleOrDefault(s =>
                        s.AccountId == packet.AccountId && s.SessionId == packet.SessionId);

                    if (Session == null)
                        response.ErrorCode = RestoreSessionError.InvalidSession;
                    else
                    {
                        Account = accountCandidate;
                        response.ErrorCode = RestoreSessionError.Success;
                        await SendPacket(response);
                        await SendMessages(packet.Channels);
                        return;
                    }
                }
                else
                    response.ErrorCode = RestoreSessionError.InvalidCredentials;
                await SendPacket(response);
            }
        }

        public async Task SendMessages(List<(long channelId, long messageId)> currentState)
        {
            using (DatabaseContext ctx = new DatabaseContext())
            {
                Channel[] channels = ctx.ChannelMembers.Where(m => m.AccountId == Account.AccountId)
                    .Join(ctx.Channels, m => m.ChannelId, c => c.ChannelId, (m, c) => c).ToArray();

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
                            packet.CounterpartId = await ctx.ChannelMembers
                                .Where(m => m.ChannelId == channel.ChannelId && m.AccountId != channel.OwnerId)
                                .Select(m => m.AccountId).SingleAsync();
                        await SendPacket(packet);
                        currentState.Add((channel.ChannelId, 0));
                    }
                }

                // Send messages from loopback channel
                Channel loopback = channels.Single(c => c.ChannelType == ChannelType.Loopback);
                long lastLoopbackMessage = currentState.Single(s => s.channelId == loopback.ChannelId).messageId;
                foreach (Message message in ctx.Messages.Where(m => m.ChannelId == loopback.ChannelId && m.MessageId > lastLoopbackMessage))
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
                foreach (Channel channel in ctx.ChannelMembers.Where(m => m.AccountId == Account.AccountId)
                    .Join(ctx.Channels, m => m.ChannelId, c => c.ChannelId, (m, c) => c).Where(c => c.ChannelType == ChannelType.Direct))
                {
                    long lastMessage = currentState.Single(s => s.channelId == channel.ChannelId).messageId;
                    foreach (Message message in ctx.Messages.Where(m => m.ChannelId == channel.ChannelId && m.MessageId > lastMessage))
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
                        else if (ctx.BlockedAccounts.Any(b => b.OwnerId == packet.CounterpartId && b.AccountId == Account.AccountId)
                                 || ctx.BlockedAccounts.Any(b => b.OwnerId == Account.AccountId && b.AccountId == packet.CounterpartId))
                            response.ErrorCode = CreateChannelError.Blocked;
                        else
                        {
                            // TODO: Check whether a direct channel exists before and after inserting
                            channel = await DatabaseHelper.AddChannel(new Channel
                            {
                                Owner = Account,
                                ChannelType = ChannelType.Direct
                            });

                            ctx.ChannelMembers.Add(new ChannelMember { ChannelId = channel.ChannelId, AccountId = Account.AccountId });
                            ctx.ChannelMembers.Add(new ChannelMember { ChannelId = channel.ChannelId, AccountId = packet.CounterpartId });
                            await ctx.SaveChangesAsync();

                            response.ErrorCode = CreateChannelError.Success;
                        }
                        break;
                    case ChannelType.Group:
                        channel = await DatabaseHelper.AddChannel(new Channel
                        {
                            Owner = Account,
                            ChannelType = packet.ChannelType
                        });

                        response.ErrorCode = CreateChannelError.Success;
                        break;
                    case ChannelType.ProfileData:
                    case ChannelType.Loopback:
                        if (ctx.Channels.Any(c => c.OwnerId == Account.AccountId && c.ChannelType == packet.ChannelType))
                            response.ErrorCode = CreateChannelError.AlreadyExists;
                        else
                        {
                            channel = await DatabaseHelper.AddChannel(new Channel
                            {
                                Owner = Account,
                                ChannelType = packet.ChannelType
                            });

                            ctx.ChannelMembers.Add(new ChannelMember { ChannelId = channel.ChannelId, AccountId = Account.AccountId });
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

        public async Task Handle(P0BChannelMessage packet)
        {
            if (packet.ContentPacketId < 0x13 || packet.ContentPacketId > 0x2A)
                throw new ProtocolException("Invalid content packet ID");

            if (packet.MessageFlags.HasFlag(MessageFlags.Unencrypted))
            {
                if (!(Packet.Packets[packet.ContentPacketId] is P0BChannelMessage message)
                    || !message.ContentPacketPolicy.HasFlag(PacketPolicy.Receive))
                    throw new ProtocolException("Content packet is no receivable channel message");

                P0BChannelMessage instance = message.Create(packet);

                using (PacketBuffer buffer = PacketBuffer.CreateStatic(packet.ContentPacket))
                    instance.ReadMessage(buffer);

                if (await instance.HandleMessage(this) != MessageSendError.Success)
                    return; // Not all messages can be saved, some return MessageSendError other than Success
            }

            Message entity = new Message
            {
                ChannelId = packet.ChannelId,
                SenderId = Account.AccountId,
                DispatchTime = DateTime.Now,
                MessageFlags = packet.MessageFlags,
                // TODO: Implement FileId
                // TODO: Implement dependencies
                ContentPacketId = packet.ContentPacketId,
                ContentPacketVersion = packet.ContentPacketVersion,
                ContentPacket = packet.ContentPacket
            };

            entity = await DatabaseHelper.AddMessage(entity);

            var response = Packet.New<P0CChannelMessageResponse>();
            response.ChannelId = packet.ChannelId;
            response.TempMessageId = packet.MessageId;
            response.ErrorCode = MessageSendError.Success;
            response.MessageId = entity.MessageId;
            // TODO: Implement skip count
            response.DispatchTime = entity.DispatchTime;
            await SendPacket(response);

            using (DatabaseContext ctx = new DatabaseContext())
            {
                packet.SenderId = Account.AccountId;
                packet.MessageId = entity.MessageId;
                packet.DispatchTime = entity.DispatchTime;
                await Program.SendAllExcept(packet, ctx.ChannelMembers
                    .Where(m => m.ChannelId == packet.ChannelId).Select(m => m.AccountId), this);
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

        public Task<MessageSendError> Handle(P18PublicKeys packet) // Alice changes her keypair
        {
            using (DatabaseContext ctx = new DatabaseContext())
            {
                foreach (Channel channel in ctx.ChannelMembers.Where(m => m.AccountId == Account.AccountId)
                    .Join(ctx.Channels, m => m.ChannelId, c => c.ChannelId, (m, c) => c)
                    .Where(c => c.ChannelType == ChannelType.Direct))
                {
                    P18PublicKeys forward = Packet.New<P18PublicKeys>();
                    forward.ChannelId = channel.ChannelId;
                    forward.SenderId = Account.AccountId;
                    forward.DispatchTime = DateTime.Now;
                    forward.MessageFlags = MessageFlags.Unencrypted | MessageFlags.NoSenderSync;
                    forward.SignatureKeyFormat = packet.SignatureKeyFormat;
                    forward.SignatureKey = packet.SignatureKey;
                    forward.DerivationKeyFormat = packet.DerivationKeyFormat;
                    forward.DerivationKey = packet.DerivationKey;
                    // TODO: Forward public keys packet with MessageFlags.NoSenderSync
                    //       We want a dependency on the original packet in the database but not sent to Bob
                    //       So we declare this dependency specific for Alice's account

                    // TODO: Get Bob's latest public key packet in this channel and resolve the dependency to Alice's keypair
                    // TODO: Resolve the dependency from Bob's private key packet and take the currently received packet of Alice
                    // TODO: Combine the packets of the last two steps and create one direct channel update
                }
            }
            return Task.FromResult(MessageSendError.Success);
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
