using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Skynet.Server.Configuration;
using Skynet.Server.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Skynet.Server.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureSkynet(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAndBindOptions<DatabaseOptions>(configuration)
                .ValidateDataAnnotations();
            services.AddAndBindOptions<FcmOptions>(configuration)
                .ValidateDataAnnotations();
            services.AddAndBindOptions<ListenerOptions>(configuration)
                .ValidateDataAnnotations();
            services.AddAndBindOptions<MailOptions>(configuration)
                .Validate(mailOptions =>
                {
                    if (!mailOptions.EnableMailing) return true;
                    ValidationContext context = new ValidationContext(mailOptions);
                    return Validator.TryValidateObject(mailOptions, context, null);
                }, "Validation of MailOptions failed");
            services.AddAndBindOptions<ProtocolOptions>(configuration)
                .ValidateDataAnnotations();
            services.AddAndBindOptions<WebOptions>(configuration)
                .ValidateDataAnnotations();

            return services;
        }

        private static OptionsBuilder<T> AddAndBindOptions<T>(this IServiceCollection services, IConfiguration configuration) where T:class 
        {
            return services.AddOptions<T>()
                .Bind(configuration.GetSection(typeof(T).Name.Replace("Options", null, StringComparison.Ordinal)));
        }

        public static IServiceCollection AddDatabaseContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContextPool<DatabaseContext>(options =>
            {
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                options.UseMySql(configuration.GetValue<string>("Database:ConnectionString"), options => options.EnableRetryOnFailure());
            });

            return services;
        }
    }
}
