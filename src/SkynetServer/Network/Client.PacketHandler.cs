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
                        passwordUpdate.MessageFlags = MessageFlags.Unencrypted;
                        await channel.SendMessage(passwordUpdate, account.AccountId);

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
                    foreach (Message message in ctx.Messages.Where(m => m.ChannelId == channel.ChannelId && m.MessageId > lastMessage)
                        .Where(m => !m.MessageFlags.HasFlag(MessageFlags.Loopback) || m.SenderId == Account.AccountId)
                        .Where(m => !m.MessageFlags.HasFlag(MessageFlags.NoSenderSync) || m.SenderId != Account.AccountId))
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
                ContentPacketId = packet.ContentPacketId,
                ContentPacketVersion = packet.ContentPacketVersion,
                ContentPacket = packet.ContentPacket
            };

            entity = await DatabaseHelper.AddMessage(entity, packet.Dependencies.ToDatabase());

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

            await packet.PostHandling(this, entity);
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
            // TODO: Validate dependencies
            return Task.FromResult(MessageSendError.Success);
        }

        public async Task PostHandling(P18PublicKeys packet, Message message) // Alice changes her keypair
        {
            using (DatabaseContext ctx = new DatabaseContext())
            {
                foreach (Channel channel in ctx.ChannelMembers.Where(m => m.AccountId == Account.AccountId)
                    .Join(ctx.Channels, m => m.ChannelId, c => c.ChannelId, (m, c) => c)
                    .Where(c => c.ChannelType == ChannelType.Direct))
                {
                    // Forward public keys packet with MessageFlags.NoSenderSync
                    // We want a dependency on the original packet in the database but not sent to Bob
                    // So we declare this dependency specific for Alice's account

                    P18PublicKeys forward = Packet.New<P18PublicKeys>();
                    forward.ChannelId = channel.ChannelId;
                    forward.SenderId = Account.AccountId;
                    forward.DispatchTime = DateTime.Now;
                    forward.MessageFlags = MessageFlags.Unencrypted | MessageFlags.NoSenderSync;
                    forward.Dependencies.Add(new Dependency(Account.AccountId, message.ChannelId, message.MessageId));
                    forward.SignatureKeyFormat = packet.SignatureKeyFormat;
                    forward.SignatureKey = packet.SignatureKey;
                    forward.DerivationKeyFormat = packet.DerivationKeyFormat;
                    forward.DerivationKey = packet.DerivationKey;
                    Message forwardMsg = await channel.SendMessage(packet, Account.AccountId);

                    long bobId = await ctx.ChannelMembers
                        .Where(m => m.ChannelId == channel.ChannelId && m.AccountId != Account.AccountId)
                        .Select(m => m.AccountId).SingleAsync();

                    // Get Bob's latest public key packet in this channel and resolve the dependency to Alice's keypair

                    Message bobPublic = await ctx.Messages
                        .Where(m => m.ChannelId == channel.ChannelId && m.ContentPacketId == 0x18 && m.SenderId == bobId)
                        .OrderByDescending(m => m.MessageId).FirstOrDefaultAsync();

                    if (bobPublic == null) continue; // The server will create the DirectChannelUpdate when Bob sends his public key

                    Message alicePrivate = await ctx.Messages
                        .SingleAsync(m => m.ChannelId == packet.Dependencies[0].ChannelId && m.MessageId == packet.Dependencies[0].MessageId);

                    var refForAlice = Packet.New<P19KeypairReference>();
                    refForAlice.MessageFlags = MessageFlags.Loopback | MessageFlags.Unencrypted;
                    refForAlice.Dependencies.Add(new Dependency(Account.AccountId, alicePrivate.ChannelId, alicePrivate.MessageId));
                    refForAlice.Dependencies.Add(new Dependency(bobId, bobPublic.ChannelId, bobPublic.MessageId));
                    Message msgForAlice = await channel.SendMessage(refForAlice, Account.AccountId);

                    // Resolve the dependency from Bob's public key packet and take the currently forwarded packet of Alice

                    Message bobPublicGlobal = await ctx.MessageDependencies
                        .Where(d => d.OwningChannelId == bobPublic.ChannelId && d.OwningMessageId == bobPublic.MessageId)
                        .Select(d => d.Message).SingleAsync();

                    Message bobPrivate = await ctx.MessageDependencies
                        .Where(d => d.OwningChannelId == bobPublicGlobal.ChannelId && d.OwningMessageId == bobPublicGlobal.MessageId)
                        .Select(d => d.Message).SingleAsync();

                    var refForBob = Packet.New<P19KeypairReference>();
                    refForAlice.MessageFlags = MessageFlags.Loopback | MessageFlags.Unencrypted;
                    refForBob.Dependencies.Add(new Dependency(bobId, bobPrivate.ChannelId, bobPrivate.MessageId));
                    refForBob.Dependencies.Add(new Dependency(Account.AccountId, forwardMsg.ChannelId, forwardMsg.MessageId));
                    Message msgForBob = await channel.SendMessage(refForBob, bobId);

                    // Combine the packets of the last two steps and create one direct channel update

                    var update = Packet.New<P1BDirectChannelUpdate>();
                    update.MessageFlags = MessageFlags.Unencrypted;
                    update.Dependencies.Add(new Dependency(Account.AccountId, msgForAlice.ChannelId, msgForAlice.MessageId));
                    update.Dependencies.Add(new Dependency(bobId, msgForBob.ChannelId, msgForBob.MessageId));
                    await channel.SendMessage(update, null);
                }
            }
        }

        public Task<MessageSendError> Handle(P1EGroupChannelUpdate packet)
        {
            // TODO: Check for concurrency issues before insert
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
