using Microsoft.Extensions.DependencyInjection;
using Skynet.Server.Services;
using System;
using System.Collections.Generic;

namespace Skynet.Server.Tests.Fakes
{
    public static class FakeServiceProvider
    {
        public static IServiceProvider Create(string databaseName)
        {
            var serviceDescriptors = new ServiceCollection();
            serviceDescriptors.ConfigureSkynetEmpty();
            serviceDescriptors.AddSingleton<IFirebaseService, FakeFirebaseService>();
            serviceDescriptors.AddSingleton<ConnectionsService>();
            serviceDescriptors.AddSingleton<PacketService>();
            serviceDescriptors.AddSingleton<NotificationService>();
            serviceDescriptors.AddTestDatabaseContext(databaseName);
            serviceDescriptors.AddScoped<DeliveryService>();

            return serviceDescriptors.BuildServiceProvider();
        }
    }
}
