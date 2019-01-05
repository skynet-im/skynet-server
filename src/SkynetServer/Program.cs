﻿using Microsoft.EntityFrameworkCore;
using SkynetServer.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SkynetServer
{
    class Program
    {
        static void Main(string[] args)
        {
            long accountId;
            long channelId;

            using (DatabaseContext ctx = new DatabaseContext())
            {
                //ctx.Database.EnsureDeleted();
                ctx.Database.Migrate();
            }

            using (DatabaseContext ctx = new DatabaseContext())
            {
                accountId = ctx.AddAccount(new Account() { AccountName = $"{new Random().Next()}@example.com" }).AccountId;
            }

            using (DatabaseContext ctx = new DatabaseContext())
            {
                channelId = ctx.AddChannel(new Channel() { OwnerId = accountId }).ChannelId;
            }

            Parallel.For(0, 1000, i =>
            {
                using (DatabaseContext ctx = new DatabaseContext())
                {
                    ctx.AddMessage(new Message() { ChannelId = channelId, SenderId = accountId, DispatchTime = DateTime.Now });
                }
            });

            Console.WriteLine("Finished saving");

            using (DatabaseContext ctx = new DatabaseContext())
            {
                foreach (Channel c in ctx.Channels)
                {
                    Console.WriteLine($"Channel with id {c.ChannelId}");

                    foreach (Message m in ctx.Messages.Where(x => x.ChannelId == c.ChannelId))
                    {
                        Console.WriteLine($"\tMessage with id {m.MessageId}");
                    }
                }
            }

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
