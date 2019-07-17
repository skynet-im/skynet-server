﻿using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using SkynetServer.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SkynetServer.Network.Mail
{
    internal class ConfirmationMailer
    {
        private readonly MailOptions config;

        public ConfirmationMailer()
        {
            config = Program.Configuration.Get<SkynetOptions>().MailOptions;
        }

        public ConfirmationMailer(MailOptions config)
        {
            this.config = config;
        }

        public async Task SendMailAsync(string address, string token)
        {
            if (!config.EnableMailing)
            {
                Console.WriteLine($"Mailing is disabled. \"{address}\" will not receive the token \"{token}\".");
                return;
            }

            ValidationContext context = new ValidationContext(config);
            if (!Validator.TryValidateObject(config, context, null))
            {
                Console.WriteLine($"Mail configuration is invalid. \"{address}\" will not receive the token \"{token}\".");
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(config.SenderName, config.SenderAddress));
            message.To.Add(new MailboxAddress(address));
            message.Subject = "Confirm your email address";
            BodyBuilder builder = new BodyBuilder
            {
                TextBody = GetMailText().Replace("$ADDRESS", address).Replace("$TOKEN", token),
                HtmlBody = GetMailHtml().Replace("$ADDRESS", address).Replace("$TOKEN", token)
            };
            message.Body = builder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(config.SmtpHost, config.SmtpPort, config.UseSsl);
                await client.AuthenticateAsync(config.SmtpUsername, config.SmtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(quit: true);
            }
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                    RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    var domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(email,
                    @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                    @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        private static string GetMailText()
        {
            Stream stream = typeof(ConfirmationMailer).Assembly.GetManifestResourceStream("SkynetServer.Resources.email.txt");
            using (StreamReader reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        private static string GetMailHtml()
        {
            Stream stream = typeof(ConfirmationMailer).Assembly.GetManifestResourceStream("SkynetServer.Resources.email.xhtml");
            using (StreamReader reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }
    }
}
