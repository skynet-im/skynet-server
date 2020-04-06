using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skynet.Server.Configuration;
using Skynet.Server.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skynet.Server.Tests
{
    [TestClass]
    public class ConfigurationTests
    {
        [TestMethod]
        public void TestConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("skynetconfig.json", optional: false, reloadOnChange: true)
                .Build();

            var descriptors = new ServiceCollection();
            descriptors.ConfigureSkynet(configuration);
            IServiceProvider provider = descriptors.BuildServiceProvider();

            T get<T>() where T : class, new()
            {
                return provider.GetService<IOptions<T>>()?.Value;
            }

            Assert.IsNotNull(get<DatabaseOptions>());
            Assert.IsNotNull(get<ListenerOptions>());
            Assert.IsNotNull(get<MailOptions>());
            Assert.IsNotNull(get<ProtocolOptions>());
            Assert.IsNotNull(get<ProtocolOptions>().Platforms);
            Assert.IsTrue(get<ProtocolOptions>().Platforms.Any());
            Assert.IsNotNull(get<WebOptions>());
        }
    }
}
