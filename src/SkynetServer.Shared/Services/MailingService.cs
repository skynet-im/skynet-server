using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using SkynetServer.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace SkynetServer.Services
{
    public class MailingService
    {
        private readonly IOptions<MailOptions> mailOptions;

        public MailingService(IOptions<MailOptions> mailOptions)
        {
            this.mailOptions = mailOptions;
        }

        public async Task SendMailAsync(string address, string token)
        {
            MailOptions config = mailOptions.Value;

            if (!config.EnableMailing)
            {
                Console.WriteLine($"Mailing is disabled. \"{address}\" will not receive the token \"{token}\".");
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(config.SenderName, config.SenderAddress));
            message.To.Add(new MailboxAddress(address));
            message.Subject = "Confirm your email address";
            BodyBuilder builder = new BodyBuilder
            {
                TextBody = GetMailText()
                    .Replace("$ADDRESS", address, StringComparison.Ordinal)
                    .Replace("$TOKEN", token, StringComparison.Ordinal),
                HtmlBody = GetMailHtml()
                    .Replace("$ADDRESS", address, StringComparison.Ordinal)
                    .Replace("$TOKEN", token, StringComparison.Ordinal)
            };
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(config.SmtpHost, config.SmtpPort, config.UseSsl).ConfigureAwait(false);
            await client.AuthenticateAsync(config.SmtpUsername, config.SmtpPassword).ConfigureAwait(false);
            await client.SendAsync(message).ConfigureAwait(false);
            await client.DisconnectAsync(quit: true).ConfigureAwait(false);
        }

        private static string GetMailText()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SkynetServer.Resources.email.txt");
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static string GetMailHtml()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SkynetServer.Resources.email.xhtml");
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
