using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using SkynetServer.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Network.Mail
{
    internal class ConfirmationMailer
    {
        public async Task SendMailAsync(string address, string token)
        {
            MailConfig config = Program.Configuration.Get<SkynetConfig>().MailConfig;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(config.SenderName, config.SenderAddress));
            message.To.Add(new MailboxAddress(address));
            message.Subject = "Confirm your email address";
            message.Body = new TextPart()
            {
                Text = config.ContentTemplate.Replace("$ADDRESS", address).Replace("$TOKEN", token)
            };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(config.SmtpHost, config.SmtpPort, config.UseSsl);
                await client.AuthenticateAsync(config.SmtpUsername, config.SmtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(quit: true);
            }
            //message.From.Add()
        }
    }
}
