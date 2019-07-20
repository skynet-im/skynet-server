﻿using Microsoft.Extensions.Configuration;
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
            services.AddOptions<MailOptions>()
                .Bind(configuration.GetSection(nameof(MailOptions)))
                .Validate(mailOptions =>
                {
                    if (!mailOptions.EnableMailing) return true;
                    ValidationContext context = new ValidationContext(mailOptions);
                    return Validator.TryValidateObject(mailOptions, context, null);
                });
            services.AddOptions<ProtocolOptions>()
                .Bind(configuration.GetSection(nameof(ProtocolOptions)))
                .ValidateDataAnnotations();
            services.AddOptions<VslOptions>()
                .Bind(configuration.GetSection(nameof(VslOptions)))
                .ValidateDataAnnotations();

            // TODO: Load DatabaseContexts via Dependency Injection
            DatabaseContext.ConnectionString = configuration.GetSection("DatabaseOptions").GetValue<string>("ConnectionString");

            return services;
        }
    }
}