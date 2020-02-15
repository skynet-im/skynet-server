using Microsoft.EntityFrameworkCore;
using SkynetServer.Database.Entities;
using SkynetServer.Model;
using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using SkynetServer.Services;
using SkynetServer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkynetServer.Network.Handlers
{
    internal class P02CreateAccountHandler : PacketHandler<P02CreateAccount>
    {
        private readonly MessageInjectionService injector;
        private readonly MailingService mailing;

        public P02CreateAccountHandler(MessageInjectionService injector, MailingService mailing)
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
                packet.AccountName = MailUtilities.SimplifyAddress(packet.AccountName);
                (var newAccount, var confirmation, bool success) = await Database.AddAccount(packet.AccountName, packet.KeyHash).ConfigureAwait(false);
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
                    passwordUpdate.MessageFlags = MessageFlags.Unencrypted;
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
