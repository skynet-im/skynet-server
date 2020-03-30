using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Skynet.Server.Extensions;
using Skynet.Server.Web.Extensions;

namespace Skynet.Server.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureSkynet(Configuration);

            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddRouting();
#if DEBUG
            services.AddMvc()
                .AddViewLocalization()
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddRazorRuntimeCompilation();
#else
            services.AddMvc()
                .AddViewLocalization()
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
#endif

            services.AddDatabaseContext(Configuration);

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var cultures = new[] { new CultureInfo("en-US"), new CultureInfo("de") };

                options.DefaultRequestCulture = new RequestCulture("en-US");
                options.SupportedCultures = cultures;
                options.SupportedUICultures = cultures;
                options.RequestCultureProviders = new[] { new AcceptLanguageHeaderRequestCultureProvider() };
            });
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Called by the runtime.")]
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/error");
            }

            app.UseProxy();
            app.UseRequestLocalization();
            app.UseStatusCodePagesWithReExecute("/status/{0}");
            app.UseStaticFiles();
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }
}
