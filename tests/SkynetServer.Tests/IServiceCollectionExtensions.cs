using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkynetServer.Database;
using System;
using System.Collections.Generic;

namespace SkynetServer.Tests
{
    internal static class IServiceCollectionExtensions
    {
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
