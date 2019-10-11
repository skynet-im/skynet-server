using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Cli.Commands
{
    [Command("skynet", Description = "Skynet Server Management Console")]
    [Subcommand(typeof(AccountCommand), typeof(DatabaseCommand))]
    [HelpOption]
    public class SkynetCommand
    {
        private int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 1;
        }
    }
}
