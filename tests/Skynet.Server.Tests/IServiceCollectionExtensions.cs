using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Skynet.Server.Configuration;
using Skynet.Server.Database;
using System;
using System.Collections.Generic;

namespace Skynet.Server.Tests
{
    internal static class IServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureSkynetEmpty(this IServiceCollection services)
        {
            services.AddOptions<DatabaseOptions>();
            services.AddOptions<FcmOptions>();
            services.AddOptions<ListenerOptions>();
            services.AddOptions<MailOptions>();
            services.AddOptions<ProtocolOptions>();
            services.AddOptions<WebOptions>();

            return services;
        }

        public static IServiceCollection AddTestDatabaseContext(this IServiceCollection services, string databaseName)
        {
            services.AddDbContext<DatabaseContext>(options =>
            {
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                options.UseInMemoryDatabase(databaseName);
            });

            return services;
        }
    }
}
