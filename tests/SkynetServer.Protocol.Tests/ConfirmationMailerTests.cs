using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkynetServer.Configuration;
using SkynetServer.Network.Mail;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkynetServer.Protocol.Tests
{
    [TestClass]
    public class ConfirmationMailerTests
    {
        [TestMethod]
        public async Task TestSendMail()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build()
                .Get<SkynetConfig>().MailConfig;

            var mailer = new ConfirmationMailer(config);
            await Assert.ThrowsExceptionAsync<SmtpCommandException>(() => mailer.SendMailAsync("anonymous@example.com", "unittest"));
        }
    }
}
