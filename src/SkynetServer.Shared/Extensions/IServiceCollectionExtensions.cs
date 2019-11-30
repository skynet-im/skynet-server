using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkynetServer.Configuration;
using SkynetServer.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SkynetServer.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureSkynet(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<SkynetOptions>()
                .Bind(configuration);
            services.AddOptions<DatabaseOptions>()
                .Bind(configuration.GetSection(nameof(DatabaseOptions)))
                .ValidateDataAnnotations();
            services.AddOptions<FcmOptions>()
                .Bind(configuration.GetSection(nameof(FcmOptions)))
                .ValidateDataAnnotations();
            services.AddOptions<ListenerOptions>()
                .Bind(configuration.GetSection(nameof(ListenerOptions)))
                .ValidateDataAnnotations();
            services.AddOptions<MailOptions>()
                .Bind(configuration.GetSection(nameof(MailOptions)))
                .Validate(mailOptions =>
                {
                    if (!mailOptions.EnableMailing) return true;
                    ValidationContext context = new ValidationContext(mailOptions);
                    return Validator.TryValidateObject(mailOptions, context, null);
                }, "Validation of MailOptions failed");
            services.AddOptions<ProtocolOptions>()
                .Bind(configuration.GetSection(nameof(ProtocolOptions)))
                .ValidateDataAnnotations();

            return services;
        }

        public static IServiceCollection AddDatabaseContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContextPool<DatabaseContext>(options =>
            {
                options.UseMySql(configuration.GetValue<string>("DatabaseOptions:ConnectionString"));
            });

            return services;
        }
    }
}
