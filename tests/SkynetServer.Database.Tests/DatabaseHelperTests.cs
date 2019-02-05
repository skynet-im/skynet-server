﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkynetServer.Database.Entities;
using SkynetServer.Model;
using SkynetServer.Threading;
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
        public async Task TestAddAccount()
        {
            await AsyncParallel.ForAsync(0, 500, async i =>
            {
                (_, _, bool success) = await DatabaseHelper.AddAccount($"{RandomAddress()}@example.com", new byte[0]);
                Assert.IsTrue(success);
            });
        }

        [TestMethod]
        public async Task TestAddExistingAccount()
        {
            const string address = "concurrency@unit.test";
            await DatabaseHelper.AddAccount(address, new byte[0]);
            (_, _, bool success) = await DatabaseHelper.AddAccount(address, new byte[0]);
            Assert.IsFalse(success);
        }

        [TestMethod]
        public async Task TestAddAccountAndSession()
        {
            await AsyncParallel.ForAsync(0, 100, async i =>
            {
                (var account, _, bool success) = await DatabaseHelper.AddAccount($"{RandomAddress()}@example.com", new byte[0]);
                Assert.IsTrue(success);

                await AsyncParallel.ForAsync(0, 10, j =>
                {
                    Session session = new Session()
                    {
                        AccountId = account.AccountId,
                        ApplicationIdentifier = "windows/SkynetServer.Database.Tests",
                        CreationTime = DateTime.Now
                    };
                    return DatabaseHelper.AddSession(session);
                });
            });
        }

        [TestMethod]
        public async Task TestAddChannel()
        {
            await AsyncParallel.ForAsync(0, 500, i =>
            {
                Channel channel = new Channel() { ChannelType = ChannelType.Loopback };
                return DatabaseHelper.AddChannel(channel);
            });
        }

        [TestMethod]
        public async Task TestAddChannelAndMessage()
        {
            await AsyncParallel.ForAsync(0, 5, async i =>
            {
                Channel channel = new Channel() { ChannelType = ChannelType.Loopback };
                await DatabaseHelper.AddChannel(channel);

                await AsyncParallel.ForAsync(0, 100, j =>
                {
                    Message message = new Message() { ChannelId = channel.ChannelId, DispatchTime = DateTime.Now };
                    return DatabaseHelper.AddMessage(message);
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
