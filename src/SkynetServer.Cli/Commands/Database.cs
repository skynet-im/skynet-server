using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using SkynetServer.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SkynetServer.Cli.Commands
{
    [Command("database")]
    [Subcommand(typeof(Create), typeof(Delete))]
    internal class Database : CommandBase
    {
        private int OnExecute(CommandLineApplication app)
        {
            app.ShowHelp();
            return 1;
        }

        [Command("create")]
        internal class Create : CommandBase
        {
            private void OnExecute()
            {
                using (DatabaseContext context = new DatabaseContext())
                {
                    context.Database.EnsureCreated();
                }
            }
        }

        [Command("delete")]
        internal class Delete : CommandBase
        {
            private void OnExecute()
            {
                if (Prompt.GetYesNo("Do you really want to delete the Skynet database?", false))
                {
                    using (DatabaseContext context = new DatabaseContext())
                    {
                        context.Database.EnsureDeleted();
                    }
                }
            }
        }
    }
}
