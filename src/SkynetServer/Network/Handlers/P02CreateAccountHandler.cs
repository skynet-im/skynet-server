﻿using Microsoft.EntityFrameworkCore;
using SkynetServer.Database;
using SkynetServer.Database.Entities;
using SkynetServer.Model;
using SkynetServer.Network.Model;
using SkynetServer.Network.Packets;
using SkynetServer.Services;
using SkynetServer.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Handlers
{
    internal class P02CreateAccountHandler : PacketHandler<P02CreateAccount>
    {
        private readonly MailingService mailing;

        public P02CreateAccountHandler(MailingService mailing)
        {
            this.mailing = mailing;
        }

        public override async ValueTask Handle(P02CreateAccount packet)
        {
            var response = Packet.New<P03CreateAccountResponse>();
            if (!MailUtilities.IsValidAddress(packet.AccountName))
                response.StatusCode = CreateAccountStatus.InvalidAccountName;
            else
            {
                packet.AccountName = MailUtilities.SimplifyAddress(packet.AccountName);
                (var newAccount, var confirmation, bool success) = await DatabaseHelper.AddAccount(packet.AccountName, packet.KeyHash);
                if (!success)
                    response.StatusCode = CreateAccountStatus.AccountNameTaken;
                else
                {
                    Task mail = mailing.SendMailAsync(confirmation.MailAddress, confirmation.Token);

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

                    Database.ChannelMembers.Add(new ChannelMember { ChannelId = loopback.ChannelId, AccountId = newAccount.AccountId });
                    Database.ChannelMembers.Add(new ChannelMember { ChannelId = accountData.ChannelId, AccountId = newAccount.AccountId });
                    await Database.SaveChangesAsync();

                    // Send password update packet
                    var passwordUpdate = Packet.New<P15PasswordUpdate>();
                    passwordUpdate.KeyHash = packet.KeyHash;
                    passwordUpdate.MessageFlags = MessageFlags.Unencrypted;
                    await Delivery.CreateMessage(passwordUpdate, loopback, newAccount.AccountId);

                    // Send email address
                    var mailAddress = Packet.New<P14MailAddress>();
                    mailAddress.MailAddress = await Database.MailConfirmations.Where(c => c.AccountId == newAccount.AccountId)
                        .Select(c => c.MailAddress).SingleAsync();
                    mailAddress.MessageFlags = MessageFlags.Unencrypted;
                    await Delivery.CreateMessage(mailAddress, accountData, newAccount.AccountId);

                    await mail;
                    response.StatusCode = CreateAccountStatus.Success;
                }
            }
            await Client.SendPacket(response);
        }
    }
}
