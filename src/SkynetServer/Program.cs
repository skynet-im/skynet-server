using SkynetServer.Entities;
using System;
using System.Linq;

namespace SkynetServer
{
    class Program
    {
        static void Main(string[] args)
        {
            using (DatabaseContext ctx = new DatabaseContext())
            {
                ctx.Database.EnsureDeleted();
                ctx.Database.EnsureCreated();
            }

            using (DatabaseContext ctx = new DatabaseContext())
            {
                Random random = new Random();
                Span<byte> value = stackalloc byte[8];
                random.NextBytes(value);
                long id = BitConverter.ToInt64(value);

                ctx.Channels.Add(new Channel() { ChannelId = id });
                ctx.Messages.Add(new Message() { ChannelId = id });
                ctx.Messages.Add(new Message() { ChannelId = id });
                ctx.Messages.Add(new Message() { ChannelId = id });
                ctx.SaveChanges();
            }

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
