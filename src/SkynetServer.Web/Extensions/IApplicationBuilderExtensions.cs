using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SkynetServer.Configuration;
using System;
using System.Collections.Generic;

namespace SkynetServer.Web.Extensions
{
    public static class IApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseProxy(this IApplicationBuilder app)
        {
            var options = app.ApplicationServices.GetRequiredService<IOptions<WebOptions>>();

            if (options.Value.AllowProxies)
            {
                var headersOptions = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.All };
                headersOptions.KnownNetworks.Clear();
                headersOptions.KnownProxies.Clear();
                app.UseForwardedHeaders(headersOptions);
            }

            app.UsePathBase(options.Value.PathBase);

            return app;
        }
    }
}
