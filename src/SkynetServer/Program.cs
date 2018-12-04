using Microsoft.EntityFrameworkCore;
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
            long id;

            using (DatabaseContext ctx = new DatabaseContext())
            {
                //ctx.Database.EnsureDeleted();
                ctx.Database.Migrate();
            }

            using (DatabaseContext ctx = new DatabaseContext())
            {
                Random random = new Random();
                Span<byte> value = stackalloc byte[8];
                random.NextBytes(value);
                id = BitConverter.ToInt64(value);

                ctx.Channels.Add(new Channel() { ChannelId = id });
                ctx.SaveChanges();
            }

            Parallel.For(0, 1000, i =>
            {
                using (DatabaseContext ctx = new DatabaseContext())
                {
                    ctx.AddMessage(new Message() { ChannelId = id, DispatchTime = DateTime.Now });
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
