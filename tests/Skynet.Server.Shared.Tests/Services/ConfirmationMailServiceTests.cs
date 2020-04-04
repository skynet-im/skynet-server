using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skynet.Server.Configuration;
using Skynet.Server.Services;
using System;
using System.Collections.Generic;

namespace Skynet.Server.Tests.Services
{
    [TestClass]
    public class ConfirmationMailServiceTests
    {
        [TestMethod]
        public void TestGetConfirmationUrl()
        {
            const string baseUrl = "https://account.skynet.app/indev/";

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOptions<WebOptions>()
                .Configure(options => options.PublicBaseUrl = new Uri(baseUrl));
            services.AddSingleton<ConfirmationMailService>();
            
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            var confirmationMailer = serviceProvider.GetRequiredService<ConfirmationMailService>();
            string confirmationUrl = confirmationMailer.GetConfirmationUrl("token").AbsoluteUri;
            Assert.AreEqual("https://account.skynet.app/indev/confirm/token", confirmationUrl);
        }
    }
}
