﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SkynetServer.Configuration;
using SkynetServer.Entities;
using SkynetServer.Network;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using VSL;

namespace SkynetServer
{
    internal static class Program
    {
        public static IConfiguration Configuration { get; private set; }
        public static ImmutableList<Client> Clients { get; private set; }
        public static readonly object ClientsLock = new object();

        static void Main(string[] args)
        {
            Configuration = new ConfigurationBuilder().Build();
            Clients = ImmutableList.Create<Client>();
            VSLListener listener = CreateListener();

            long accountId;
            long channelId;

            using (DatabaseContext ctx = new DatabaseContext())
            {
                ctx.Database.EnsureDeleted();
                ctx.Database.Migrate();
            }

            using (DatabaseContext ctx = new DatabaseContext())
            {
                Account account = new Account() { AccountName = $"{new Random().Next()}@example.com", KeyHash = new byte[0] };
                accountId = ctx.AddAccount(account).AccountId;
                MailConfirmation confirmation = ctx.AddMailConfirmation(account, account.AccountName);
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

        private static VSLListener CreateListener()
        {
            VslConfig config = Configuration.Get<VslConfig>();
            IPEndPoint[] endPoints = {
                new IPEndPoint(IPAddress.Any, config.TcpPort),
                new IPEndPoint(IPAddress.IPv6Any, config.TcpPort)
            };

            SocketSettings settings = new SocketSettings()
            {
                LatestProductVersion = config.LatestProductVersion,
                OldestProductVersion = config.OldestProductVersion,
                RsaXmlKey = config.RsaXmlKey
            };

            return new VSLListener(endPoints, settings, AcceptClient);
        }

        private static void AcceptClient(VSLServer socket)
        {
            Client client = new Client(socket);
            lock (ClientsLock) Clients = Clients.Add(client);
            client.Start();
        }
    }
}
