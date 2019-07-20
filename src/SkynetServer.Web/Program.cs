using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace SkynetServer.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(config =>
                {
                    // Unlike all other .NET projects, ASP.NET Core uses the project root as working directory instead of the build directory.
                    // For debugging we have to reference the config source file from SkynetServer.Shared.
                    FileInfo primary = new FileInfo("skynetconfig.json");
                    FileInfo secondary = new FileInfo(Path.Join("..", "SkynetServer.Shared", "skynetconfig.json"));
                    if (primary.Exists)
                        config.AddJsonFile(primary.FullName, optional: false, reloadOnChange: true);
                    else if (secondary.Exists)
                        config.AddJsonFile(secondary.FullName, optional: false, reloadOnChange: true);
                    else
                        throw new FileNotFoundException("Configuration file not found", primary.FullName);
                })
                .UseStartup<Startup>();
    }
}
