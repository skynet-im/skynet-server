using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Cli.Commands
{
    [Command("skynet")]
    [Subcommand(typeof(Database))]
    internal class Skynet : CommandBase
    {
        private int OnExecute(IConsole console)
        {
            console.Error.WriteLine("False skynet command");
            return 1;
        }
    }
}
