using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkynetServer.Configuration;
using SkynetServer.Services;
using System;
using System.Collections.Generic;

namespace SkynetServer.Tests.Services
{
    [TestClass]
    public class ConfirmationMailServiceTests
    {
        [TestMethod]
        public void TestGetConfirmationUrl()
        {
            const string baseUrl = "https://account.skynet.app/indev/";

            var webOptions = new FakeOptions<WebOptions>(new WebOptions
            {
                PublicBaseUrl = new Uri(baseUrl)
            });

            var confirmationMailer = new ConfirmationMailService(null, webOptions);
            string confirmationUrl = confirmationMailer.GetConfirmationUrl("token").AbsoluteUri;
            Assert.AreEqual("https://account.skynet.app/indev/confirm/token", confirmationUrl);
        }

        private class FakeOptions<T> : IOptions<T> where T : class, new()
        {
            public FakeOptions(T value)
            {
                Value = value;
            }

            public T Value { get; }
        }
    }
}
