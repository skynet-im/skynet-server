using McMaster.Extensions.CommandLineUtils;
using SkynetServer.Cli.Commands;
using System;

namespace SkynetServer.Cli
{
    internal static class Program
    {
        static int Main(string[] args)
        {
            return CommandLineApplication.Execute<Skynet>(args);
        }
    }
}
