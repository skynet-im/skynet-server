using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using SkynetServer.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
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
            await client.ConnectAsync(config.SmtpHost, config.SmtpPort, config.UseSsl);
            await client.AuthenticateAsync(config.SmtpUsername, config.SmtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(quit: true);
        }

        public string SimplifyAddress(string address)
        {
            return address.Replace("@googlemail.com", "@gmail.com", StringComparison.Ordinal);
        }

        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                    RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                static string DomainMapper(Match match)
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

        private string GetMailText()
        {
            Stream stream = GetType().Assembly.GetManifestResourceStream("SkynetServer.Resources.email.txt");
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private string GetMailHtml()
        {
            Stream stream = GetType().Assembly.GetManifestResourceStream("SkynetServer.Resources.email.xhtml");
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
