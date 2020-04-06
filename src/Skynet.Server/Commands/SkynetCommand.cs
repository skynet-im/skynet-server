using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable IDE0051 // Remove unused private members

namespace Skynet.Server.Commands
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
