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
                    (var newAccount, var confirmation, bool success) = await DatabaseHelper.AddAccount(packet.AccountName, packet.KeyHash);
                    if (!success)
                        response.ErrorCode = CreateAccountError.AccountNameTaken;
                    else
                    {
                        Task mail = new ConfirmationMailer().SendMailAsync(confirmation.MailAddress, confirmation.Token);

                        Channel loopback = await DatabaseHelper.AddChannel(new Channel
                        {
                            ChannelType = ChannelType.Loopback,
                            OwnerId = newAccount.AccountId
                        });
                        Channel accountData = await DatabaseHelper.AddChannel(new Channel
                        {
                            ChannelType = ChannelType.AccountData,
                            OwnerId = newAccount.AccountId
                        });

                        ctx.ChannelMembers.Add(new ChannelMember { ChannelId = loopback.ChannelId, AccountId = newAccount.AccountId });
                        ctx.ChannelMembers.Add(new ChannelMember { ChannelId = accountData.ChannelId, AccountId = newAccount.AccountId });
                        await ctx.SaveChangesAsync();

                        // Send password update packet
                        var passwordUpdate = Packet.New<P15PasswordUpdate>();
                        passwordUpdate.KeyHash = packet.KeyHash;
                        passwordUpdate.MessageFlags = MessageFlags.Unencrypted;
                        await loopback.SendMessage(passwordUpdate, newAccount.AccountId);

                        // Send email address
                        var mailAddress = Packet.New<P14MailAddress>();
                        mailAddress.MailAddress = await ctx.MailConfirmations.Where(c => c.AccountId == newAccount.AccountId)
                            .Select(c => c.MailAddress).SingleAsync();
                        mailAddress.MessageFlags = MessageFlags.Unencrypted;
                        await accountData.SendMessage(mailAddress, newAccount.AccountId);

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
                        Session.LastConnected = DateTime.Now;
                        await ctx.SaveChangesAsync();
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
                                .Where(m => m.ChannelId == channel.ChannelId && m.AccountId != Account.AccountId)
                                .Select(m => m.AccountId).SingleAsync();
                        await SendPacket(packet);
                        currentState.Add((channel.ChannelId, 0));
                    }
                }

                // Send messages from loopback channel
                Channel loopback = channels.Single(c => c.ChannelType == ChannelType.Loopback);
                long lastLoopbackMessage = currentState.Single(s => s.channelId == loopback.ChannelId).messageId;
                foreach (Message message in ctx.Messages
                    .Where(m => m.ChannelId == loopback.ChannelId && m.MessageId > lastLoopbackMessage)
                    .Include(m => m.Dependencies).OrderBy(m => m.MessageId))
                {
                    await SendPacket(message.ToPacket());
                }

                // Send messages from account data channels
                foreach (long channelId in ctx.ChannelMembers.Where(m => m.AccountId == Account.AccountId)
                    .Join(ctx.Channels, m => m.ChannelId, c => c.ChannelId, (m, c) => c)
                    .Where(c => c.ChannelType == ChannelType.AccountData).Select(c => c.ChannelId))
                {
                    long lastMessage = currentState.Single(s => s.channelId == channelId).messageId;
                    await SendMessages(channelId, lastMessage);
                }

                // Send messages from direct channels
                foreach (long channelId in ctx.ChannelMembers.Where(m => m.AccountId == Account.AccountId)
                    .Join(ctx.Channels, m => m.ChannelId, c => c.ChannelId, (m, c) => c)
                    .Where(c => c.ChannelType == ChannelType.Direct).Select(c => c.ChannelId))
                {
                    long lastMessage = currentState.Single(s => s.channelId == channelId).messageId;
                    await SendMessages(channelId, lastMessage);
                }

                await SendPacket(Packet.New<P0FSyncFinished>());
            }
        }

        private async Task SendMessages(long channelId, long lastMessage)
        {
            using (DatabaseContext ctx = new DatabaseContext())
            {
                foreach (Message message in ctx.Messages
                    .Where(m => m.ChannelId == channelId && m.MessageId > lastMessage)
                    .Where(m => !m.MessageFlags.HasFlag(MessageFlags.Loopback) || m.SenderId == Account.AccountId)
                    .Where(m => !m.MessageFlags.HasFlag(MessageFlags.NoSenderSync) || m.SenderId != Account.AccountId)
                    .Include(m => m.Dependencies).OrderBy(m => m.MessageId))
                {
                    await SendPacket(message.ToPacket());
                }
            }
        }

        public async Task Handle(P0ACreateChannel packet)
        {
            using (var ctx = new DatabaseContext())
            {
                Channel channel = null;
                var response = Packet.New<P2FCreateChannelResponse>();
                response.TempChannelId = packet.ChannelId;

                switch (packet.ChannelType)
                {
                    case ChannelType.Loopback:
                        throw new ProtocolException("Loopback channels cannot be created manually");
                    case ChannelType.AccountData:
                        throw new ProtocolException("Account data channels cannot be created manually");
                    case ChannelType.Direct:
                        var counterpart = await ctx.Accounts.SingleOrDefaultAsync(acc => acc.AccountId == packet.CounterpartId);
                        if (counterpart == null)
                        {
                            response.ErrorCode = CreateChannelError.InvalidCounterpart;
                            await SendPacket(response);
                        }
                        else if (await ctx.BlockedAccounts.AnyAsync(b => b.OwnerId == packet.CounterpartId && b.AccountId == Account.AccountId)
                            || await ctx.BlockedAccounts.AnyAsync(b => b.OwnerId == Account.AccountId && b.AccountId == packet.CounterpartId))
                        {
                            response.ErrorCode = CreateChannelError.Blocked;
                            await SendPacket(response);
                        }
                        else if (await ctx.ChannelMembers.Where(m => m.AccountId == packet.CounterpartId)
                            .Join(ctx.ChannelMembers.Where(m => m.AccountId == Account.AccountId)
                                .Join(ctx.Channels, m => m.ChannelId, c => c.ChannelId, (m, c) => c)
                                .Where(c => c.ChannelType == ChannelType.Direct),
                                m => m.ChannelId, c => c.ChannelId, (m, c) => c)
                            .AnyAsync())
                        {
                            response.ErrorCode = CreateChannelError.AlreadyExists;
                            await SendPacket(response);
                        }
                        else
                        {
                            // Create a new direct channel
                            channel = await DatabaseHelper.AddChannel(new Channel
                            {
                                OwnerId = Account.AccountId,
                                ChannelType = ChannelType.Direct
                            });

                            ctx.ChannelMembers.Add(new ChannelMember { ChannelId = channel.ChannelId, AccountId = Account.AccountId });
                            ctx.ChannelMembers.Add(new ChannelMember { ChannelId = channel.ChannelId, AccountId = packet.CounterpartId });
                            await ctx.SaveChangesAsync();

                            // TODO: Check for existing direct channels and delete if another channel was created in the meantime

                            var createAlice = Packet.New<P0ACreateChannel>();
                            createAlice.ChannelId = channel.ChannelId;
                            createAlice.ChannelType = ChannelType.Direct;
                            createAlice.OwnerId = Account.AccountId;
                            createAlice.CounterpartId = packet.CounterpartId;
                            await createAlice.SendTo(Account.AccountId, this);

                            var createBob = Packet.New<P0ACreateChannel>();
                            createBob.ChannelId = channel.ChannelId;
                            createBob.ChannelType = ChannelType.Direct;
                            createBob.OwnerId = Account.AccountId;
                            createBob.CounterpartId = Account.AccountId;
                            await createBob.SendTo(packet.CounterpartId, null);

                            response.ErrorCode = CreateChannelError.Success;
                            response.ChannelId = channel.ChannelId;
                            await SendPacket(response);

                            await ForwardAccountChannels(ctx, Account, counterpart);

                            Message alicePublic = await Account.GetLatestPublicKey(ctx);
                            Message bobPublic = await counterpart.GetLatestPublicKey(ctx);

                            await CreateKeyReferences(ctx, channel, Account.AccountId, alicePublic, counterpart.AccountId, bobPublic);
                        }
                        break;
                    case ChannelType.Group:
                    case ChannelType.ProfileData:
                        throw new NotImplementedException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private async Task ForwardAccountChannels(DatabaseContext ctx, Account alice, Account bob)
        {
            Channel aliceChannel = await ctx.Channels.SingleAsync(c => c.ChannelType == ChannelType.AccountData && c.OwnerId == alice.AccountId);
            Channel bobChannel = await ctx.Channels.SingleAsync(c => c.ChannelType == ChannelType.AccountData && c.OwnerId == bob.AccountId);

            ctx.ChannelMembers.Add(new ChannelMember { ChannelId = aliceChannel.ChannelId, AccountId = bob.AccountId });
            ctx.ChannelMembers.Add(new ChannelMember { ChannelId = bobChannel.ChannelId, AccountId = alice.AccountId });
            await ctx.SaveChangesAsync();

            var createAlice = Packet.New<P0ACreateChannel>();
            createAlice.ChannelId = bobChannel.ChannelId;
            createAlice.ChannelType = ChannelType.AccountData;
            createAlice.OwnerId = bob.AccountId;
            await createAlice.SendTo(alice.AccountId, null);

            var createBob = Packet.New<P0ACreateChannel>();
            createBob.ChannelId = aliceChannel.ChannelId;
            createBob.ChannelType = ChannelType.AccountData;
            createBob.OwnerId = alice.AccountId;
            await createBob.SendTo(bob.AccountId, null);

            await Task.WhenAll(SendAllMessages(bobChannel, alice), SendAllMessages(aliceChannel, bob));
        }

        private async Task SendAllMessages(Channel channel, Account account)
        {
            using (DatabaseContext ctx = new DatabaseContext())
            {
                // TODO: Skip accounts with no connected user
                foreach (Message message in ctx.Messages
                    .Where(m => m.ChannelId == channel.ChannelId)
                    .Where(m => !m.MessageFlags.HasFlag(MessageFlags.Loopback) || m.SenderId == account.AccountId)
                    .Where(m => !m.MessageFlags.HasFlag(MessageFlags.NoSenderSync) || m.SenderId != account.AccountId)
                    .Include(m => m.Dependencies).OrderBy(m => m.MessageId))
                {
                    await message.ToPacket().SendTo(account.AccountId, null);
                }
            }
        }

        private async Task CreateKeyReferences(DatabaseContext ctx, Channel channel, long aliceId, Message alicePublic, long bobId, Message bobPublic)
        {
            Message alicePrivate = await ctx.MessageDependencies
                .Where(d => d.OwningChannelId == alicePublic.ChannelId && d.OwningMessageId == alicePublic.MessageId)
                .Select(d => d.Message).SingleAsync();

            var refForAlice = Packet.New<P19KeypairReference>();
            refForAlice.MessageFlags = MessageFlags.Loopback | MessageFlags.Unencrypted;
            refForAlice.Dependencies.Add(new Dependency(aliceId, alicePrivate.ChannelId, alicePrivate.MessageId));
            refForAlice.Dependencies.Add(new Dependency(bobId, bobPublic.ChannelId, bobPublic.MessageId));
            Message msgForAlice = await channel.SendMessage(refForAlice, Account.AccountId);

            Message bobPrivate = await ctx.MessageDependencies
                .Where(d => d.OwningChannelId == bobPublic.ChannelId && d.OwningMessageId == bobPublic.MessageId)
                .Select(d => d.Message).SingleAsync();

            var refForBob = Packet.New<P19KeypairReference>();
            refForAlice.MessageFlags = MessageFlags.Loopback | MessageFlags.Unencrypted;
            refForBob.Dependencies.Add(new Dependency(bobId, bobPrivate.ChannelId, bobPrivate.MessageId));
            refForBob.Dependencies.Add(new Dependency(aliceId, alicePublic.ChannelId, alicePublic.MessageId));
            Message msgForBob = await channel.SendMessage(refForBob, bobId);

            // Combine the packets of the last two steps and create one direct channel update

            var update = Packet.New<P1BDirectChannelUpdate>();
            update.MessageFlags = MessageFlags.Unencrypted;
            update.Dependencies.Add(new Dependency(aliceId, msgForAlice.ChannelId, msgForAlice.MessageId));
            update.Dependencies.Add(new Dependency(bobId, msgForBob.ChannelId, msgForBob.MessageId));
            await channel.SendMessage(update, null);
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

            // TODO: Check if the account has the permission to send messages in this channel

            Message entity = new Message
            {
                ChannelId = packet.ChannelId,
                SenderId = Account.AccountId,
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
            response.DispatchTime = DateTime.SpecifyKind(entity.DispatchTime, DateTimeKind.Local);
            await SendPacket(response);

            using (DatabaseContext ctx = new DatabaseContext())
            {
                packet.SenderId = Account.AccountId;
                packet.MessageId = entity.MessageId;
                packet.DispatchTime = DateTime.SpecifyKind(entity.DispatchTime, DateTimeKind.Local);

                if (packet.ContentPacketId == 0x20)
                {
                    IEnumerable<Session> sessions = ctx.ChannelMembers
                        .Where(m => m.ChannelId == packet.ChannelId)
                        .Join(ctx.Sessions, m => m.AccountId, s => s.AccountId, (m, s) => s);
                    await delivery.SendPacketOrNotify(packet, sessions, exclude: this, excludeFcm: Account.AccountId);
                }
                else
                {
                    IEnumerable<long> accounts = ctx.ChannelMembers
                        .Where(m => m.ChannelId == packet.ChannelId)
                        .Select(m => m.AccountId);
                    await packet.SendTo(accounts, exclude: this);
                }
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
                // Get all direct channels of Alice
                foreach (Channel channel in ctx.ChannelMembers
                    .Where(m => m.AccountId == Account.AccountId)
                    .Join(ctx.Channels, m => m.ChannelId, c => c.ChannelId, (m, c) => c)
                    .Where(c => c.ChannelType == ChannelType.Direct))
                {
                    Account bob = await ctx.ChannelMembers
                        .Where(m => m.ChannelId == channel.ChannelId && m.AccountId != Account.AccountId)
                        .Select(m => m.Account).SingleAsync();

                    // Get Bob's latest public key packet in this channel and take Alice's new public key

                    Message bobPublic = await bob.GetLatestPublicKey(ctx);

                    if (bobPublic == null) continue; // The server will create the DirectChannelUpdate when Bob sends his public key

                    await CreateKeyReferences(ctx, channel, Account.AccountId, message, bob.AccountId, bobPublic);
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
                    .Where(c => c.AccountId != Account.AccountId
                        && c.MailAddress.Contains(packet.Query)
                        && c.ConfirmationTime != default) // Exclude unconfirmed accounts
                    .Take(100); // Limit to 100 entries
                var response = Packet.New<P2ESearchAccountResponse>();
                foreach (var result in results)
                    response.Results.Add(new SearchResult(result.AccountId, result.MailAddress));
                // Forward public packets to fully implement the Skynet protocol v5
                return SendPacket(response);
            }
        }

        public Task Handle(P30FileUpload packet)
        {
            throw new NotImplementedException();
        }
    }
}
