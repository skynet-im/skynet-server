using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkynetServer.Entities;
using SkynetServer.Model;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Wiry.Base32;

namespace SkynetServer.Database.Tests
{
    [TestClass]
    public class DatabaseHelperTests
    {
        [TestMethod]
        public void TestAddAccount()
        {
            Parallel.For(0, 500, i =>
            {
                Account account = new Account() { AccountName = $"{RandomAddress()}@example.com", KeyHash = new byte[0] };
                DatabaseHelper.AddAccount(account);
            });
        }

        [TestMethod]
        public void TestAddAccountAndSession()
        {
            Parallel.For(0, 100, i =>
            {
                Account account = new Account() { AccountName = $"{RandomAddress()}@example.com", KeyHash = new byte[0] };
                DatabaseHelper.AddAccount(account);

                Parallel.For(0, 10, j =>
                {
                    Session session = new Session()
                    {
                        AccountId = account.AccountId,
                        ApplicationIdentifier = "windows/SkynetServer.Database.Tests",
                        CreationTime = DateTime.Now
                    };
                    DatabaseHelper.AddSession(session);
                });
            });
        }

        [TestMethod]
        public void TestAddChannel()
        {
            Parallel.For(0, 500, i =>
            {
                Channel channel = new Channel() { ChannelType = ChannelType.Loopback };
                DatabaseHelper.AddChannel(channel);
            });
        }

        [TestMethod]
        public void TestAddChannelAndMessage()
        {
            Parallel.For(0, 50, i =>
            {
                Channel channel = new Channel() { ChannelType = ChannelType.Loopback };
                DatabaseHelper.AddChannel(channel);

                Parallel.For(0, 500, j =>
                {
                    Message message = new Message() { ChannelId = channel.ChannelId, DispatchTime = DateTime.Now };
                    DatabaseHelper.AddMessage(message);
                });
            });
        }

        private string RandomAddress()
        {
            using (var random = RandomNumberGenerator.Create())
            {
                byte[] value = new byte[10];
                random.GetBytes(value);
                return Base32Encoding.Standard.GetString(value).ToLower();
            }
        }
    }
}
