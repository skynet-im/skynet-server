using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Skynet.Server.Database;
using System;
using System.Collections.Generic;

namespace Skynet.Server.Tests
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
