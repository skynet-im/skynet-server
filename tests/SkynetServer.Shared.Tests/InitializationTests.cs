using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkynetServer.Configuration;
using SkynetServer.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Shared.Tests
{
    [TestClass]
    public class InitializationTests
    {
        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            DatabaseContext.ConnectionString = configuration.Get<SkynetOptions>().DatabaseOptions.ConnectionString;

            using (DatabaseContext ctx = new DatabaseContext())
            {
                ctx.Database.EnsureCreated();
            }
        }

        [TestMethod]
        public void TestConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var options = configuration.Get<SkynetOptions>();
            Assert.IsNotNull(options);
            Assert.IsNotNull(options.DatabaseOptions);
            Assert.IsNotNull(options.MailOptions);
            Assert.IsNotNull(options.ProtocolOptions);
            Assert.IsNotNull(options.ProtocolOptions.Platforms);
            Assert.IsTrue(options.ProtocolOptions.Platforms.Count > 0);
            Assert.IsNotNull(options.VslOptions);
        }
    }
}
