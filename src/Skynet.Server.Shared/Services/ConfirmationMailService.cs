﻿using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Skynet.Server.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Skynet.Server.Services
{
    public class ConfirmationMailService
    {
        private readonly IOptions<MailOptions> mailOptions;
        private readonly IOptions<WebOptions> webOptions;
        private readonly ILogger<ConfirmationMailService> logger;

        public ConfirmationMailService(IOptions<MailOptions> mailOptions, IOptions<WebOptions> webOptions, ILogger<ConfirmationMailService> logger)
        {
            this.mailOptions = mailOptions;
            this.webOptions = webOptions;
            this.logger = logger;
        }

        public Uri GetConfirmationUrl(string token)
        {
            return new Uri(webOptions.Value.PublicBaseUrl, "confirm/" + token);
        }
        
        public async Task SendMailAsync(string address, string token)
        {
            MailOptions config = mailOptions.Value;

            if (!config.EnableMailing)
            {
                logger.LogWarning("Mailing is disabled. \"{0}\" will not receive the token \"{1}\".", address, token);
                return;
            }

            string url = GetConfirmationUrl(token).AbsoluteUri;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(config.SenderName, config.SenderAddress));
            message.To.Add(new MailboxAddress(address));
            message.Subject = "Confirm your email address";
            BodyBuilder builder = new BodyBuilder
            {
                TextBody = GetMailText()
                    .Replace("$ADDRESS", address, StringComparison.Ordinal)
                    .Replace("$URL", url, StringComparison.Ordinal),
                HtmlBody = GetMailHtml()
                    .Replace("$ADDRESS", address, StringComparison.Ordinal)
                    .Replace("$URL", url, StringComparison.Ordinal)
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
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Skynet.Server.Resources.email.txt");
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static string GetMailHtml()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Skynet.Server.Resources.email.xhtml");
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
