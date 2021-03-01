using Microsoft.EntityFrameworkCore;
using Skynet.Model;
using Skynet.Protocol.Model;
using Skynet.Protocol.Packets;
using Skynet.Server.Database.Entities;
using Skynet.Server.Services;
using Skynet.Server.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Skynet.Server.Network.Handlers
{
    internal class P02CreateAccountHandler : PacketHandler<P02CreateAccount>
    {
        private readonly MessageInjectionService injector;
        private readonly ConfirmationMailService mailing;

        public P02CreateAccountHandler(MessageInjectionService injector, ConfirmationMailService mailing)
        {
            this.injector = injector;
            this.mailing = mailing;
        }

        public override async ValueTask Handle(P02CreateAccount packet)
        {
            var response = Packets.New<P03CreateAccountResponse>();
            if (!MailUtilities.IsValidAddress(packet.AccountName))
                response.StatusCode = CreateAccountStatus.InvalidAccountName;
            else
            {
                using var csp = SHA256.Create();
                byte[] passwordHash = csp.ComputeHash(packet.KeyHash);

                // As of RFC 5321 the local-part of an email address should not be case-sensitive.
                (var newAccount, var confirmation, bool success) = 
                    await Database.AddAccount(packet.AccountName.ToLowerInvariant(), passwordHash).ConfigureAwait(false);
                if (!success)
                    response.StatusCode = CreateAccountStatus.AccountNameTaken;
                else
                {
                    Task mail = mailing.SendMailAsync(confirmation.MailAddress, confirmation.Token);

                    Channel loopback = await Database.AddChannel(
                        new Channel
                        {
                            ChannelType = ChannelType.Loopback,
                            OwnerId = newAccount.AccountId
                        }, 
                        new ChannelMember { AccountId = newAccount.AccountId })
                        .ConfigureAwait(false);

                    Channel accountData = await Database.AddChannel(
                        new Channel
                        {
                            ChannelType = ChannelType.AccountData,
                            OwnerId = newAccount.AccountId
                        },
                        new ChannelMember { AccountId = newAccount.AccountId })
                        .ConfigureAwait(false);

                    // Create password update packet
                    var passwordUpdate = Packets.New<P15PasswordUpdate>();
                    passwordUpdate.KeyHash = packet.KeyHash;
                    passwordUpdate.MessageFlags = MessageFlags.Loopback | MessageFlags.Unencrypted;
                    _ = await injector.CreateMessage(passwordUpdate, loopback.ChannelId, newAccount.AccountId).ConfigureAwait(false);

                    // Create email address
                    var mailAddress = Packets.New<P14MailAddress>();
                    mailAddress.MailAddress = await Database.MailConfirmations.AsQueryable()
                        .Where(c => c.AccountId == newAccount.AccountId)
                        .Select(c => c.MailAddress).SingleAsync().ConfigureAwait(false);
                    mailAddress.MessageFlags = MessageFlags.Unencrypted;
                    _ = await injector.CreateMessage(mailAddress, accountData.ChannelId, newAccount.AccountId).ConfigureAwait(false);

                    // As newly created account do not have sessions or contacts there is no need to deliver these packets immediately

                    await mail.ConfigureAwait(false);
                    response.StatusCode = CreateAccountStatus.Success;
                }
            }
            await Client.Send(response).ConfigureAwait(false);
        }
    }
}
